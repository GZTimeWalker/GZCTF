using System.Formats.Tar;
using System.IO.Compression;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GZCTF.Services.Transfer;

public sealed record ChallengeImportOptions(int GameId, Guid SubmitterUserId, bool AutoApprove);

public sealed record ChallengeImportResult(
    int Imported,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<string> Messages);

/// <summary>
/// Imports challenges from either a single-challenge tarball upload or a
/// public github repo. Both paths share the per-challenge core (parse
/// YAML → validate → upload attachment → upsert challenge + flags +
/// attachment via the repository layer).
///
/// Recurring github watches call <see cref="ImportFromGitHubAsync"/> on a
/// schedule (see <see cref="RepoWatchService"/>). User submissions and
/// admin one-shots call it through controller endpoints.
/// </summary>
public sealed class ChallengeImportService(
    IGameRepository gameRepository,
    IGameChallengeRepository challengeRepository,
    IBlobRepository blobRepository,
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<ChallengeImportService> logger)
{
    static readonly HashSet<string> IgnoredDirNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "solver", "solvers", "dist", "node_modules", "writeup", "writeups"
    };

    static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Imports a single-challenge tarball. Caller supplies a stream of a
    /// gzipped tar containing one <c>challenge.yaml</c> at the root (or
    /// inside one wrapping directory — common when downloads come from
    /// "Save as" on GitHub).
    /// </summary>
    public async Task<ChallengeImportResult> ImportFromArchiveAsync(
        Stream archive, ChallengeImportOptions opts, CancellationToken token)
    {
        var workDir = CreateWorkDir();
        try
        {
            await ExtractTarballAsync(archive, workDir, token);
            return await ImportFromWorkDirAsync(workDir, subpath: null, opts, token);
        }
        finally
        {
            TryDeleteDir(workDir);
        }
    }

    /// <summary>
    /// Imports every <c>challenge.yaml</c> / <c>.yml</c> file found under
    /// the optional <see cref="GitHubLocator.Subpath"/> in the github repo
    /// identified by <paramref name="loc"/>. Pass <paramref name="githubToken"/>
    /// for private repos; null is fine for public ones.
    /// </summary>
    public async Task<ChallengeImportResult> ImportFromGitHubAsync(
        GitHubLocator loc, string? githubToken, ChallengeImportOptions opts, CancellationToken token)
    {
        var http = httpClientFactory.CreateClient("GitHubApi");
        var workDir = CreateWorkDir();
        try
        {
            await using (var tarStream = await loc.DownloadTarballAsync(http, githubToken, token))
                await ExtractTarballAsync(tarStream, workDir, token);

            return await ImportFromWorkDirAsync(workDir, loc.Subpath, opts, token);
        }
        finally
        {
            TryDeleteDir(workDir);
        }
    }

    private async Task<ChallengeImportResult> ImportFromWorkDirAsync(
        string workDir, string? subpath, ChallengeImportOptions opts, CancellationToken token)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == opts.GameId, token);
        if (game is null)
            return new(0, 0, 0, 1, ["Game not found."]);

        var scanRoot = ResolveScanRoot(workDir, subpath);
        if (scanRoot is null)
            return new(0, 0, 0, 1, [$"Subpath '{subpath}' not found inside the archive."]);

        var packageRoots = FindChallengePackages(scanRoot).ToList();
        if (packageRoots.Count == 0)
            return new(0, 0, 0, 1, ["No challenge.yaml / challenge.yml files found."]);

        var messages = new List<string>();
        int imported = 0, updated = 0, skipped = 0, failed = 0;

        foreach (var (yamlPath, packageDir) in packageRoots)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var outcome = await ImportOneAsync(game, packageDir, yamlPath, opts, token);
                switch (outcome.Kind)
                {
                    case OutcomeKind.Created: imported++; break;
                    case OutcomeKind.Updated: updated++; break;
                    case OutcomeKind.Skipped: skipped++; messages.Add(outcome.Message ?? "skipped"); break;
                }
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "ChallengeImport: failed at {Path}", yamlPath);
                messages.Add($"{Path.GetRelativePath(workDir, packageDir)}: {ex.Message}");
            }
        }

        return new(imported, updated, skipped, failed, messages);
    }

    private enum OutcomeKind { Created, Updated, Skipped }
    private sealed record Outcome(OutcomeKind Kind, string? Message);

    private async Task<Outcome> ImportOneAsync(
        Game game, string packageDir, string yamlPath,
        ChallengeImportOptions opts, CancellationToken token)
    {
        var yaml = await File.ReadAllTextAsync(yamlPath, token);
        ChallengeYamlModel? model;
        try
        {
            model = YamlDeserializer.Deserialize<ChallengeYamlModel>(yaml);
        }
        catch (Exception ex)
        {
            return new(OutcomeKind.Skipped, $"Invalid YAML: {ex.Message}");
        }

        if (model is null || string.IsNullOrWhiteSpace(model.Name))
            return new(OutcomeKind.Skipped, "challenge.yaml missing 'name'");

        if (!Enum.TryParse<ChallengeType>(model.Type ?? "", true, out var type))
            return new(OutcomeKind.Skipped, $"Unknown challenge type '{model.Type}'");

        // Container challenges with a local-path image are rejected until
        // server-side docker build is wired up.
        var image = model.Container?.ContainerImage?.Trim();
        if (type.IsContainer() && !string.IsNullOrEmpty(image) && IsLocalDockerfilePath(image))
            return new(OutcomeKind.Skipped,
                $"'{model.Name}': server-side docker build is not enabled. Publish the image to a registry first.");

        var existing = await context.GameChallenges
            .Include(c => c.Flags)
            .Include(c => c.Attachment)
            .FirstOrDefaultAsync(c => c.GameId == game.Id && c.Title == model.Name, token);

        GameChallenge challenge;
        OutcomeKind kind;
        if (existing is null)
        {
            challenge = new GameChallenge
            {
                Title = model.Name!,
                Type = type,
                Category = ParseCategory(model.Category)
            };
            challenge = await challengeRepository.CreateChallenge(game, challenge, token);
            kind = OutcomeKind.Created;
        }
        else
        {
            challenge = existing;
            kind = OutcomeKind.Updated;
        }

        ApplyYamlToChallenge(challenge, model, type, image, opts);
        await context.SaveChangesAsync(token);

        await SyncFlagsAsync(challenge, model.Flags ?? [], token);
        await SyncAttachmentAsync(challenge, packageDir, model.Provide, token);

        return new(kind, null);
    }

    private static void ApplyYamlToChallenge(
        GameChallenge c, ChallengeYamlModel m, ChallengeType type, string? image,
        ChallengeImportOptions opts)
    {
        c.Title = m.Name!;
        c.Category = ParseCategory(m.Category);
        c.Content = string.IsNullOrEmpty(m.Author)
            ? (m.Description ?? string.Empty)
            : $"Author: **{m.Author}**\n\n{m.Description ?? string.Empty}";
        c.Hints = m.Hints;
        c.OriginalScore = m.Value ?? c.OriginalScore;
        c.MinScoreRate = m.MinScoreRate ?? c.MinScoreRate;
        c.Difficulty = m.Difficulty ?? c.Difficulty;
        c.SubmissionLimit = m.SubmissionLimit ?? c.SubmissionLimit;
        c.DisableBloodBonus = m.DisableBloodBonus ?? c.DisableBloodBonus;
        c.FlagTemplate = m.Container?.FlagTemplate ?? m.FlagTemplate ?? c.FlagTemplate;

        if (type.IsContainer())
        {
            c.ContainerImage = image ?? c.ContainerImage;
            c.MemoryLimit = m.Container?.MemoryLimit ?? c.MemoryLimit;
            c.CPUCount = m.Container?.CpuCount ?? c.CPUCount;
            c.StorageLimit = m.Container?.StorageLimit ?? c.StorageLimit;
            c.ExposePort = m.Container?.ExposePort ?? c.ExposePort;
            if (Enum.TryParse<NetworkMode>(m.Container?.NetworkMode ?? "", true, out var nm))
                c.NetworkMode = nm;
            c.EnableTrafficCapture = m.Container?.EnableTrafficCapture ?? c.EnableTrafficCapture;
        }

        c.IsEnabled = m.Visible ?? c.IsEnabled;
        c.ReviewStatus = opts.AutoApprove ? ChallengeReviewStatus.Active : ChallengeReviewStatus.Pending;
        c.SubmittedByUserId ??= opts.SubmitterUserId;
        c.SubmittedAtUtc ??= DateTimeOffset.UtcNow;
        if (opts.AutoApprove)
            c.ReviewedAtUtc = DateTimeOffset.UtcNow;
    }

    private async Task SyncFlagsAsync(GameChallenge challenge, List<string> desired, CancellationToken token)
    {
        var existing = challenge.Flags.Select(f => f.Flag).ToHashSet();
        var toAdd = desired.Where(f => !existing.Contains(f))
            .Select(f => new FlagCreateModel { Flag = f })
            .ToArray();
        if (toAdd.Length > 0)
            await challengeRepository.AddFlags(challenge, toAdd, token);
    }

    private async Task SyncAttachmentAsync(
        GameChallenge challenge, string packageDir, string? provide, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(provide))
            return;

        var rel = provide.Trim().Replace('\\', '/').TrimStart('/');
        if (rel.Contains("..") || Path.IsPathRooted(rel))
            throw new InvalidOperationException("'provide' path must be relative and may not contain '..'.");

        var absolute = Path.GetFullPath(Path.Combine(packageDir, rel));
        var canonicalPkg = Path.GetFullPath(packageDir) + Path.DirectorySeparatorChar;
        if (!absolute.StartsWith(canonicalPkg, StringComparison.Ordinal))
            throw new InvalidOperationException("'provide' path escapes the challenge package.");

        if (!File.Exists(absolute))
            throw new FileNotFoundException($"'provide' file not found: {rel}");

        await using var fs = File.OpenRead(absolute);
        var blob = await blobRepository.CreateOrUpdateBlobFromStream(Path.GetFileName(absolute), fs, token);

        await challengeRepository.UpdateAttachment(challenge,
            new AttachmentCreateModel
            {
                AttachmentType = FileType.Local,
                FileHash = blob.Hash
            }, token);
    }

    // ---------------- helpers ----------------

    private static string CreateWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gzctf-chal-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDeleteDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
        catch { /* best effort */ }
    }

    /// <summary>
    /// Extracts a gzipped tar stream into <paramref name="workDir"/>,
    /// rejecting any entry whose normalized path escapes the work dir.
    /// </summary>
    internal static async Task ExtractTarballAsync(Stream tarGz, string workDir, CancellationToken token)
    {
        await using var gz = new GZipStream(tarGz, CompressionMode.Decompress, leaveOpen: true);
        await using var tar = new TarReader(gz);

        var canonical = Path.GetFullPath(workDir) + Path.DirectorySeparatorChar;

        while (await tar.GetNextEntryAsync(cancellationToken: token) is { } entry)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            // tarballs from GitHub include a top-level wrapping dir like
            // `{owner}-{repo}-{sha}/...` — we just let it through; callers
            // walk for challenge.yaml so the wrapper is invisible.
            var dest = Path.GetFullPath(Path.Combine(workDir, entry.Name));
            if (!dest.StartsWith(canonical, StringComparison.Ordinal))
                throw new InvalidOperationException($"Tar entry escapes work dir: {entry.Name}");

            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(dest);
                    break;
                case TarEntryType.RegularFile:
                case TarEntryType.V7RegularFile:
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    await using (var fs = File.Create(dest))
                        if (entry.DataStream is not null)
                            await entry.DataStream.CopyToAsync(fs, token);
                    break;
                // SymbolicLink / HardLink / others: deliberately skipped.
            }
        }
    }

    private static string? ResolveScanRoot(string workDir, string? subpath)
    {
        // GitHub tarballs wrap content in `{owner}-{repo}-{sha}` — if there's
        // exactly one directory at workDir root, descend into it.
        var top = Directory.EnumerateFileSystemEntries(workDir).Take(2).ToArray();
        var root = workDir;
        if (top.Length == 1 && Directory.Exists(top[0]))
            root = top[0];

        if (string.IsNullOrEmpty(subpath))
            return root;

        var sub = subpath.Replace('\\', '/').Trim('/');
        var candidate = Path.GetFullPath(Path.Combine(root, sub));
        var canonicalRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(canonicalRoot, StringComparison.Ordinal) || !Directory.Exists(candidate))
            return null;
        return candidate;
    }

    /// <summary>
    /// Enumerates every <c>challenge.yaml</c> / <c>challenge.yml</c> below
    /// the scan root, skipping directories whose basename starts with
    /// '.' (e.g. <c>.example</c>, <c>.structure</c>, <c>.github</c>) and
    /// the conventional helper dirs (solver / dist / node_modules / …).
    /// </summary>
    private static IEnumerable<(string YamlPath, string PackageDir)> FindChallengePackages(string scanRoot)
    {
        var stack = new Stack<string>();
        stack.Push(scanRoot);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            foreach (var name in new[] { "challenge.yaml", "challenge.yml" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    yield return (candidate, dir);
                    // A package root is a *leaf* — don't descend into solver/, dist/, etc.
                    goto nextDir;
                }
            }

            foreach (var child in Directory.EnumerateDirectories(dir))
            {
                var basename = Path.GetFileName(child);
                if (string.IsNullOrEmpty(basename)) continue;
                if (basename.StartsWith('.')) continue;
                if (IgnoredDirNames.Contains(basename)) continue;
                stack.Push(child);
            }

            nextDir: ;
        }
    }

    private static ChallengeCategory ParseCategory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return ChallengeCategory.Misc;
        return Enum.TryParse<ChallengeCategory>(raw, ignoreCase: true, out var cat)
            ? cat
            : ChallengeCategory.Misc;
    }

    private static bool IsLocalDockerfilePath(string image)
    {
        var v = image.Trim();
        if (v.StartsWith("./") || v.StartsWith("../") || v.StartsWith('/')) return true;
        if (string.Equals(v, "Dockerfile", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.EndsWith("/Dockerfile", StringComparison.OrdinalIgnoreCase)) return true;
        // Anything with a registry-shape (host/name:tag or name:tag or name) we accept.
        return false;
    }
}
