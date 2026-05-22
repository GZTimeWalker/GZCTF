using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GZCTF.Services.Transfer;

internal static class GameRepoBindingProtection
{
    public const string Purpose = "GZCTF.GameRepoBinding.GitHubToken";
}

public sealed record RepoBindingScanResult(
    int GamesCreated,
    int GamesUpdated,
    int ChallengesImported,
    int ChallengesUpdated,
    int Failures,
    IReadOnlyList<string> Messages);

/// <summary>
/// One-shot discovery for a <see cref="GameRepoBinding"/>: downloads the
/// repo tarball, walks for every <c>.gzevent</c>, materialises the games
/// they describe, then chains into <see cref="ChallengeImportService"/>
/// scoped to each event root so the per-challenge import pipeline runs
/// for every challenge.yaml underneath.
/// </summary>
public sealed class RepoBindingDiscoveryService(
    AppDbContext context,
    IGameRepository gameRepository,
    ChallengeImportService challengeImporter,
    GitRepoSyncService gitSync,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<RepoBindingDiscoveryService> logger)
{
    static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(GameRepoBindingProtection.Purpose);

    static string Sanitize(string? raw, string? secret)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        if (string.IsNullOrEmpty(secret)) return raw;
        return raw.Replace(secret, "***");
    }

    /// <summary>
    /// Push a live activity message to the binding row using a direct
    /// SQL UPDATE so it commits independently of whatever transaction
    /// the scan body is holding. Failures are swallowed — losing one
    /// progress update is far better than crashing the scan.
    /// </summary>
    async Task SetActivityAsync(int bindingId, string? activity, CancellationToken token)
    {
        try
        {
            // Truncate to the column cap so a long challenge name can't
            // blow up the update with a varchar overflow.
            if (activity is { Length: > 250 }) activity = activity[..250] + "…";
            await context.GameRepoBindings
                .Where(b => b.Id == bindingId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CurrentActivity, activity), token);
            logger.LogInformation(
                "RepoBindingDiscovery: binding {Id} activity → {Activity}", bindingId, activity ?? "(idle)");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "RepoBindingDiscovery: failed to update activity for binding {Id}", bindingId);
        }
    }

    /// <param name="force">When true, skips the
    /// <c>sha == LastCommitSha</c> short-circuit and re-downloads /
    /// re-imports unconditionally. Use this for explicit "Scan now"
    /// requests where the operator's intent is to rebuild everything
    /// even though git didn't move (e.g. recovering from a partial
    /// scan or testing the import pipeline).</param>
    public async Task<RepoBindingScanResult> ScanAsync(int bindingId, Guid adminUserId, CancellationToken token, bool force = false)
    {
        var binding = await context.GameRepoBindings.FirstOrDefaultAsync(b => b.Id == bindingId, token);
        if (binding is null)
            return new(0, 0, 0, 0, 1, ["Binding not found."]);

        await SetActivityAsync(bindingId, "Starting scan", token);

        if (!GitHubLocator.TryParse(binding.RepoUrl, binding.Ref, overrideSubpath: null, out var loc, out var parseErr) || loc is null)
        {
            binding.LastScanUtc = DateTimeOffset.UtcNow;
            binding.LastScanMessage = parseErr ?? "Invalid repo URL.";
            await context.SaveChangesAsync(token);
            return new(0, 0, 0, 0, 1, [binding.LastScanMessage!]);
        }

        string? plaintextToken = null;
        if (!string.IsNullOrEmpty(binding.GitHubTokenEncrypted))
        {
            try
            {
                plaintextToken = _protector.Unprotect(binding.GitHubTokenEncrypted);
                binding.TokenStatus = TokenStatus.Ok;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RepoBindingDiscovery: failed to decrypt token for binding {Id}", bindingId);
                binding.LastScanUtc = DateTimeOffset.UtcNow;
                binding.LastScanMessage = "Stored token could not be decrypted.";
                binding.TokenStatus = TokenStatus.DecryptFailed;
                await context.SaveChangesAsync(token);
                return new(0, 0, 0, 0, 1, [binding.LastScanMessage!]);
            }
        }
        else
        {
            binding.TokenStatus = TokenStatus.NotConfigured;
        }

        var messages = new List<string>();
        int gamesCreated = 0, gamesUpdated = 0, challengesImported = 0, challengesUpdated = 0, failures = 0;
        string? sha = null;

        try
        {
            // Switched from the tarball API to a persistent shallow git
            // clone. First scan = `git clone --depth 1` (one full
            // payload). Every subsequent scan = `git fetch --depth 1`
            // which is a small delta. The result is checked out at
            // /app/repos/binding/{id} and survives container restarts
            // via the gzctf-repos docker volume.
            await SetActivityAsync(bindingId, "Syncing git checkout", token);
            var snapshot = await gitSync.SyncAsync("binding", bindingId, loc, plaintextToken, token);
            sha = snapshot.CommitSha;

            // Short-circuit when nothing changed. Cheaper than before
            // (no separate API call needed — the fetch+rev-parse above
            // already gives us the SHA). Force=true (admin "Scan now"
            // intends to re-import even on a no-op) bypasses this.
            if (!force && !string.IsNullOrEmpty(sha) && sha == binding.LastCommitSha)
            {
                await SetActivityAsync(bindingId, $"Up to date ({sha[..7]})", token);
                binding.LastScanUtc = DateTimeOffset.UtcNow;
                binding.LastScanMessage = $"No change since {sha[..7]} — skipped import.";
                await WriteScanRowAsync(bindingId, sha, 0, 0, 0, 0, 0,
                    ["No change — skipped."], plaintextToken, token);
                await context.SaveChangesAsync(token);
                return new(0, 0, 0, 0, 0, ["No change — skipped import."]);
            }

            var scanRoot = snapshot.CheckoutPath;

            await SetActivityAsync(bindingId, "Discovering .gzevent manifests", token);
            var manifests = Directory.EnumerateFiles(scanRoot, ".gzevent", SearchOption.AllDirectories)
                .ToList();
            if (manifests.Count == 0)
                messages.Add("No .gzevent manifests found in repo.");

            int manifestIdx = 0;
            foreach (var manifestPath in manifests)
            {
                manifestIdx++;
                try
                {
                    var rel = Path.GetRelativePath(scanRoot, manifestPath).Replace('\\', '/');
                    await SetActivityAsync(bindingId,
                        $"Processing event {manifestIdx}/{manifests.Count}: {rel}", token);
                    var eventRootDir = Path.GetDirectoryName(manifestPath)!;
                    var eventRootRel = Path.GetRelativePath(scanRoot, eventRootDir).Replace('\\', '/');
                    if (eventRootRel == ".") eventRootRel = "";

                    GzEventModel? manifest;
                    try
                    {
                        manifest = YamlDeserializer.Deserialize<GzEventModel>(await File.ReadAllTextAsync(manifestPath, token));
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        messages.Add(Sanitize($"{rel}: invalid YAML — {ex.Message}", plaintextToken));
                        continue;
                    }
                    if (manifest is null || string.IsNullOrWhiteSpace(manifest.Title))
                    {
                        failures++;
                        messages.Add($"{rel}: manifest missing 'title'.");
                        continue;
                    }

                    var (game, created) = await UpsertGameAsync(binding, manifest, rel, token);
                    if (created) gamesCreated++; else gamesUpdated++;

                    // Bypass ChallengeImportService.ImportFromGitHubAsync —
                    // that path would re-download the tarball for each
                    // event. We already have the entire checkout on
                    // disk from git, so just hand the same workdir +
                    // event subpath to the work-dir importer directly.
                    // For a 2-event repo this drops 2 redundant
                    // multi-MB downloads per scan.
                    var importResult = await challengeImporter.ImportFromWorkDirAsync(
                        scanRoot,
                        string.IsNullOrEmpty(eventRootRel) ? null : eventRootRel,
                        new ChallengeImportOptions(game.Id, adminUserId, AutoApprove: true),
                        originalArchiveBlobPath: null,
                        token);

                    challengesImported += importResult.Imported;
                    challengesUpdated += importResult.Updated;
                    failures += importResult.Failed;
                    foreach (var m in importResult.Messages)
                        messages.Add(Sanitize($"{rel}: {m}", plaintextToken));
                }
                catch (Exception ex)
                {
                    failures++;
                    logger.LogError(ex, "RepoBindingDiscovery: failed at {Manifest}", manifestPath);
                    messages.Add(Sanitize($"{manifestPath}: {ex.Message}", plaintextToken));
                }
            }

            binding.LastScanUtc = DateTimeOffset.UtcNow;
            binding.LastCommitSha = sha;
            binding.LastScanMessage = Sanitize(
                $"games +{gamesCreated} ~{gamesUpdated}, challenges +{challengesImported} ~{challengesUpdated}, failures {failures}",
                plaintextToken);
            await WriteScanRowAsync(bindingId, sha, gamesCreated, gamesUpdated,
                challengesImported, challengesUpdated, failures, messages, plaintextToken, token);
            await context.SaveChangesAsync(token);
            return new(gamesCreated, gamesUpdated, challengesImported, challengesUpdated, failures, messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RepoBindingDiscovery: top-level error for binding {Id}", bindingId);
            binding.LastScanUtc = DateTimeOffset.UtcNow;
            binding.LastScanMessage = Sanitize(ex.Message, plaintextToken);
            messages.Add(Sanitize(ex.Message, plaintextToken));
            await WriteScanRowAsync(bindingId, null, gamesCreated, gamesUpdated,
                challengesImported, challengesUpdated, failures + 1, messages, plaintextToken, token);
            await context.SaveChangesAsync(token);
            return new(gamesCreated, gamesUpdated, challengesImported, challengesUpdated, failures + 1,
                messages);
        }
        finally
        {
            await SetActivityAsync(bindingId, null, CancellationToken.None);
            // No workDir cleanup — the git checkout under /app/repos is
            // intentionally persistent so the next scan can `git fetch`
            // instead of cloning fresh.
        }
    }

    async Task WriteScanRowAsync(
        int bindingId, string? sha,
        int gamesCreated, int gamesUpdated,
        int challengesImported, int challengesUpdated,
        int failures, IReadOnlyList<string> messages,
        string? plaintextToken, CancellationToken token)
    {
        // Sanitize once at the boundary so the persisted row can never
        // carry the plaintext PAT, even if a downstream library managed
        // to slip it into an exception message.
        var joined = string.Join('\n', messages.Select(m => Sanitize(m, plaintextToken)));
        // Cap the persisted column at the schema's MaxLength to avoid
        // a SaveChangesAsync explosion on huge multi-event repos.
        if (joined.Length > 30000) joined = joined[..30000] + "\n…(truncated)";

        context.GameRepoBindingScans.Add(new GameRepoBindingScan
        {
            BindingId = bindingId,
            RanAtUtc = DateTimeOffset.UtcNow,
            CommitSha = sha,
            GamesCreated = gamesCreated,
            GamesUpdated = gamesUpdated,
            ChallengesImported = challengesImported,
            ChallengesUpdated = challengesUpdated,
            Failures = failures,
            Messages = string.IsNullOrEmpty(joined) ? null : joined
        });
        await Task.CompletedTask;
    }

    async Task<(Game game, bool created)> UpsertGameAsync(
        GameRepoBinding binding, GzEventModel manifest, string manifestRel, CancellationToken token)
    {
        var existing = await context.Games.FirstOrDefaultAsync(
            g => g.RepoBindingId == binding.Id && g.EventManifestPath == manifestRel, token);

        // Orphan adoption: if no game is bound to this (bindingId,
        // manifestPath), look for a detached game whose Title matches
        // the manifest's title and adopt it instead of creating a
        // duplicate. This is the recovery path for delete-then-rebind
        // — the delete endpoint by default keeps games but nulls their
        // RepoBindingId, and without this we'd end up with two games
        // of the same name (old detached + new bound).
        if (existing is null && !string.IsNullOrWhiteSpace(manifest.Title))
        {
            var orphan = await context.Games.FirstOrDefaultAsync(
                g => g.RepoBindingId == null && g.Title == manifest.Title, token);
            if (orphan is not null)
            {
                logger.LogInformation(
                    "RepoBindingDiscovery: adopting detached game {GameId} ('{Title}') for binding {BindingId}",
                    orphan.Id, orphan.Title, binding.Id);
                orphan.RepoBindingId = binding.Id;
                orphan.EventManifestPath = manifestRel;
                existing = orphan;
            }
        }

        // PG's "timestamp with time zone" via Npgsql only accepts Offset=0
        // (UTC). The .gzevent schema uses ISO-8601 with local offsets like
        // +08:00, so normalize everything before persisting.
        static DateTimeOffset Utc(DateTimeOffset v) => v.ToUniversalTime();

        if (existing is null)
        {
            var fresh = new Game
            {
                Title = manifest.Title!,
                Summary = manifest.Summary ?? string.Empty,
                Content = manifest.Content ?? string.Empty,
                Hidden = manifest.Hidden ?? false,
                PracticeMode = manifest.PracticeMode ?? true,
                AcceptWithoutReview = manifest.AcceptWithoutReview ?? false,
                InviteCode = string.IsNullOrEmpty(manifest.InviteCode) ? null : manifest.InviteCode,
                StartTimeUtc = Utc(manifest.Start ?? DateTimeOffset.UtcNow.AddDays(1)),
                EndTimeUtc = Utc(manifest.End ?? DateTimeOffset.UtcNow.AddDays(30)),
                WriteupDeadline = Utc(manifest.WriteupDeadline ?? DateTimeOffset.UtcNow.AddDays(30)),
                WriteupRequired = manifest.WriteupRequired ?? false,
                WriteupNote = manifest.WriteupNote ?? string.Empty,
                TeamMemberCountLimit = manifest.TeamMemberCountLimit ?? 0,
                ContainerCountLimit = manifest.ContainerCountLimit ?? 3,
                BloodBonus = BloodBonus.FromValue(manifest.BloodBonus ?? BloodBonus.DefaultValue),
                RepoBindingId = binding.Id,
                EventManifestPath = manifestRel
            };
            var created = await gameRepository.CreateGame(fresh, token) ?? fresh;
            return (created, true);
        }

        // Update in place.
        existing.Title = manifest.Title!;
        existing.Summary = manifest.Summary ?? existing.Summary;
        existing.Content = manifest.Content ?? existing.Content;
        if (manifest.Hidden is { } hidden) existing.Hidden = hidden;
        if (manifest.PracticeMode is { } pm) existing.PracticeMode = pm;
        if (manifest.AcceptWithoutReview is { } awr) existing.AcceptWithoutReview = awr;
        if (manifest.InviteCode is { } ic) existing.InviteCode = string.IsNullOrEmpty(ic) ? null : ic;
        if (manifest.Start is { } start) existing.StartTimeUtc = Utc(start);
        if (manifest.End is { } end) existing.EndTimeUtc = Utc(end);
        if (manifest.WriteupDeadline is { } wd) existing.WriteupDeadline = Utc(wd);
        if (manifest.WriteupRequired is { } wr) existing.WriteupRequired = wr;
        if (manifest.WriteupNote is { } wn) existing.WriteupNote = wn;
        if (manifest.TeamMemberCountLimit is { } tml) existing.TeamMemberCountLimit = tml;
        if (manifest.ContainerCountLimit is { } ccl) existing.ContainerCountLimit = ccl;
        if (manifest.BloodBonus is { } bb) existing.BloodBonus = BloodBonus.FromValue(bb);
        await context.SaveChangesAsync(token);
        return (existing, false);
    }
}
