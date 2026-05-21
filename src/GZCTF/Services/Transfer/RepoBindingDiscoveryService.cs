using System.Formats.Tar;
using System.IO.Compression;
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
    IHttpClientFactory httpClientFactory,
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

    public async Task<RepoBindingScanResult> ScanAsync(int bindingId, Guid adminUserId, CancellationToken token)
    {
        var binding = await context.GameRepoBindings.FirstOrDefaultAsync(b => b.Id == bindingId, token);
        if (binding is null)
            return new(0, 0, 0, 0, 1, ["Binding not found."]);

        var http = httpClientFactory.CreateClient("GitHubApi");

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

        var workDir = Path.Combine(Path.GetTempPath(), $"gzctf-binding-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var messages = new List<string>();
        int gamesCreated = 0, gamesUpdated = 0, challengesImported = 0, challengesUpdated = 0, failures = 0;

        try
        {
            string? sha = await loc.GetHeadShaAsync(http, plaintextToken, token);

            await using (var tarStream = await loc.DownloadTarballAsync(http, plaintextToken, token))
            await using (var gz = new GZipStream(tarStream, CompressionMode.Decompress, leaveOpen: false))
            await using (var tar = new TarReader(gz))
            {
                var canonical = Path.GetFullPath(workDir) + Path.DirectorySeparatorChar;
                while (await tar.GetNextEntryAsync(cancellationToken: token) is { } entry)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    var dest = Path.GetFullPath(Path.Combine(workDir, entry.Name));
                    if (!dest.StartsWith(canonical, StringComparison.Ordinal)) continue;

                    if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        await using var fs = File.Create(dest);
                        if (entry.DataStream is not null)
                            await entry.DataStream.CopyToAsync(fs, token);
                    }
                    else if (entry.EntryType is TarEntryType.Directory)
                    {
                        Directory.CreateDirectory(dest);
                    }
                }
            }

            // Github tarballs nest content under a single wrapper dir
            // named {owner}-{repo}-{shortsha}. Descend into it.
            var top = Directory.EnumerateFileSystemEntries(workDir).Take(2).ToArray();
            var scanRoot = top.Length == 1 && Directory.Exists(top[0]) ? top[0] : workDir;

            var manifests = Directory.EnumerateFiles(scanRoot, ".gzevent", SearchOption.AllDirectories)
                .ToList();
            if (manifests.Count == 0)
                messages.Add("No .gzevent manifests found in repo.");

            foreach (var manifestPath in manifests)
            {
                try
                {
                    var rel = Path.GetRelativePath(scanRoot, manifestPath).Replace('\\', '/');
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

                    // Now import challenges under the event root.
                    if (!GitHubLocator.TryParse(binding.RepoUrl, binding.Ref,
                        string.IsNullOrEmpty(eventRootRel) ? null : eventRootRel, out var subLoc, out var subErr) || subLoc is null)
                    {
                        failures++;
                        messages.Add($"{rel}: {subErr ?? "could not build subpath locator"}");
                        continue;
                    }

                    var importResult = await challengeImporter.ImportFromGitHubAsync(
                        subLoc, plaintextToken,
                        new ChallengeImportOptions(game.Id, adminUserId, AutoApprove: true),
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
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
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
