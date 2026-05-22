using System.Formats.Tar;
using System.IO.Compression;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Build;
using GZCTF.Storage.Interface;
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
    IBlobStorage blobStorage,
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IChallengeBuildQueue buildQueue,
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
    /// Imports a single-challenge archive — either a <c>.tar.gz</c> or a
    /// <c>.zip</c>. The format is auto-detected from magic bytes; callers
    /// don't need to disambiguate. Archive must contain one
    /// <c>challenge.yaml</c> at the root (or inside one wrapping
    /// directory — common with "Download ZIP" from GitHub).
    /// </summary>
    public async Task<ChallengeImportResult> ImportFromArchiveAsync(
        Stream archive, ChallengeImportOptions opts, CancellationToken token)
    {
        var workDir = CreateWorkDir();
        try
        {
            // Spool to a temp file so we can both upload to blob (for audit)
            // and extract without re-reading the upload stream twice.
            var spool = Path.Combine(workDir, "__upload.bin");
            await using (var fs = File.Create(spool))
                await archive.CopyToAsync(fs, token);

            var blobPath = $"imports/{opts.GameId}/{Guid.NewGuid():N}.bin";
            await using (var fs = File.OpenRead(spool))
                await blobStorage.WriteAsync(blobPath, fs, append: false, token);

            await using (var fs = File.OpenRead(spool))
                await ExtractArchiveAsync(fs, workDir, token);

            return await ImportFromWorkDirAsync(workDir, subpath: null, opts, blobPath, token);
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
                await ExtractTarballStreamAsync(tarStream, workDir, token);

            // github imports don't get an OriginalArchiveBlobPath — the
            // source is inherently public and the repo URL alone is the
            // audit trail.
            return await ImportFromWorkDirAsync(workDir, loc.Subpath, opts, originalArchiveBlobPath: null, token);
        }
        finally
        {
            TryDeleteDir(workDir);
        }
    }

    /// <summary>
    /// Import every <c>challenge.yaml</c> under <paramref name="workDir"/>
    /// (optionally scoped to a subpath). Made internal so
    /// <see cref="RepoBindingDiscoveryService"/> can call it directly
    /// against a git checkout without re-downloading a tarball.
    /// </summary>
    internal async Task<ChallengeImportResult> ImportFromWorkDirAsync(
        string workDir, string? subpath, ChallengeImportOptions opts,
        string? originalArchiveBlobPath, CancellationToken token)
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
                var outcome = await ImportOneAsync(game, packageDir, yamlPath, opts, originalArchiveBlobPath, token);
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
        ChallengeImportOptions opts, string? originalArchiveBlobPath, CancellationToken token)
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

        // Decide what build intent this challenge implies — distinct
        // from "what status should we set right now". We resolve before
        // touching the DB so the persisted state is internally
        // consistent: a row never appears in BuildStatus=Queued without
        // an actual job in the channel.
        var image = model.Container?.ContainerImage?.Trim();
        var intent = ResolveBuildIntent(type, image, packageDir);

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
                Category = ParseCategory(model.Category, packageDir)
            };
            challenge = await challengeRepository.CreateChallenge(game, challenge, token);
            kind = OutcomeKind.Created;
        }
        else
        {
            challenge = existing;
            kind = OutcomeKind.Updated;
        }

        // Apply build-intent decision to the challenge row up-front.
        // Note: enqueue happens AFTER SaveChanges so the row id exists.
        switch (intent.Kind)
        {
            case BuildIntentKind.NotApplicable:
                challenge.BuildStatus = ChallengeBuildStatus.NotApplicable;
                challenge.LastBuildLog = null;
                break;
            case BuildIntentKind.MissingDockerfile:
                challenge.BuildStatus = ChallengeBuildStatus.MissingDockerfile;
                challenge.LastBuildLog = intent.Diagnostic;
                break;
            case BuildIntentKind.BuildNeeded:
                // BuildStatus assignment moved below — set only AFTER
                // a successful enqueue so we never leave the row in
                // Queued without a corresponding job.
                break;
            // None: leave whatever was there (manual challenges keep
            // their prior status).
        }

        ApplyYamlToChallenge(challenge, model, type, image, opts, packageDir);
        if (originalArchiveBlobPath is not null)
            challenge.OriginalArchiveBlobPath = originalArchiveBlobPath;
        await context.SaveChangesAsync(token);

        await SyncFlagsAsync(challenge, model.Flags ?? [], token);
        await SyncAttachmentAsync(challenge, packageDir, model.Provide, token);

        if (intent.Kind == BuildIntentKind.BuildNeeded)
        {
            // Snapshot-then-enqueue-then-persist. The status flip only
            // happens AFTER the queue accepts the job. Without this
            // ordering, a crash between SaveChangesAsync and Enqueue
            // could leave the row in Queued with no actual job, and
            // only the next app restart's ResetStuckBuildsAsync would
            // notice. Also handles the dedup case (AlreadyPending) so
            // a re-import while a previous build is still running
            // doesn't kick off a second one.
            string? snap = null;
            try
            {
                snap = PrepareBuildSnapshot(intent.ContextDir!);
                var enqueueResult = buildQueue.Enqueue(new ChallengeBuildJob(
                    challenge.Id, game.Id, model.Name!,
                    snap, intent.Dockerfile!,
                    BuildTrigger.Import));

                switch (enqueueResult)
                {
                    case EnqueueResult.Enqueued:
                        challenge.BuildStatus = ChallengeBuildStatus.Queued;
                        challenge.LastBuildLog = null;
                        await context.SaveChangesAsync(token);
                        break;
                    case EnqueueResult.AlreadyPending:
                        // Existing build will satisfy this re-import too.
                        // Don't touch BuildStatus, don't keep the snapshot.
                        SafeDelete(snap);
                        break;
                    case EnqueueResult.Rejected:
                        SafeDelete(snap);
                        challenge.BuildStatus = ChallengeBuildStatus.Failed;
                        challenge.LastBuildLog = "Build queue is full — try again in a moment.";
                        await context.SaveChangesAsync(token);
                        logger.LogError("ChallengeImportService: queue full, rejected build for {Challenge}", model.Name);
                        return new(OutcomeKind.Skipped,
                            $"'{model.Name}': build queue full");
                }
            }
            catch (Exception ex)
            {
                if (snap is not null) SafeDelete(snap);
                challenge.BuildStatus = ChallengeBuildStatus.Failed;
                challenge.LastBuildLog = $"Failed to enqueue build: {ex.Message}";
                await context.SaveChangesAsync(token);
                logger.LogError(ex, "ChallengeImportService: enqueue failed for {Challenge}", model.Name);
                return new(OutcomeKind.Skipped,
                    $"'{model.Name}': failed to enqueue build — {ex.Message}");
            }
        }

        if (intent.Kind == BuildIntentKind.MissingDockerfile)
            return new(OutcomeKind.Skipped,
                $"'{model.Name}': Dockerfile not found at '{image}'.");

        return new(kind, null);
    }

    private enum BuildIntentKind { None, NotApplicable, MissingDockerfile, BuildNeeded }

    private sealed record BuildIntent(
        BuildIntentKind Kind,
        string? ContextDir = null,
        string? Dockerfile = null,
        string? Diagnostic = null);

    /// <summary>
    /// Decide whether the imported challenge should trigger an image
    /// build, ship as-is on a registry image, or surface a "Dockerfile
    /// missing" diagnostic. Splitting this out of the inline check
    /// inside <see cref="ImportOneAsync"/> makes the three new build
    /// statuses (NotApplicable / Queued / MissingDockerfile) explicit
    /// and testable.
    /// </summary>
    private static BuildIntent ResolveBuildIntent(ChallengeType type, string? image, string packageDir)
    {
        if (!type.IsContainer() || string.IsNullOrEmpty(image))
            return new BuildIntent(BuildIntentKind.None);

        if (!IsLocalDockerfilePath(image))
            return new BuildIntent(BuildIntentKind.NotApplicable);

        var (contextDir, dockerfile) = ResolveBuildContext(packageDir, image);
        if (!File.Exists(Path.Combine(contextDir, dockerfile)))
            return new BuildIntent(BuildIntentKind.MissingDockerfile,
                Diagnostic: $"Dockerfile not found at '{image}' (resolved to '{Path.Combine(contextDir, dockerfile)}').");

        return new BuildIntent(BuildIntentKind.BuildNeeded, contextDir, dockerfile);
    }

    /// <summary>
    /// Copy <paramref name="contextDir"/> to a fresh temp dir that
    /// outlives the import scan. Returned path is the new context root;
    /// the build queue worker is responsible for deleting it after the
    /// build completes (regardless of outcome).
    /// </summary>
    private static string PrepareBuildSnapshot(string contextDir)
    {
        var dst = Path.Combine(Path.GetTempPath(),
            "gzctf-build-" + Guid.NewGuid().ToString("N"));
        CopyDirRecursive(contextDir, dst);
        return dst;
    }

    private static void CopyDirRecursive(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.EnumerateFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
        foreach (var d in Directory.EnumerateDirectories(src))
            CopyDirRecursive(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    /// <summary>
    /// Best-effort cleanup of a snapshot dir when the enqueue path
    /// decides not to keep it (AlreadyPending dedup hit, channel full,
    /// or any exception after PrepareBuildSnapshot).
    /// </summary>
    private static void SafeDelete(string? dir)
    {
        if (string.IsNullOrEmpty(dir)) return;
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
        catch { /* swallow — best effort */ }
    }

    /// <summary>
    /// Resolve the <c>container_image</c> path declared in challenge.yaml
    /// to a (contextDir, dockerfileRelPath) pair suitable for
    /// <c>docker build</c>:
    /// <list type="bullet">
    ///   <item><description><c>./src</c>             → context = <c>{packageDir}/src</c>, dockerfile = <c>Dockerfile</c></description></item>
    ///   <item><description><c>./Dockerfile</c>      → context = <c>{packageDir}</c>,     dockerfile = <c>Dockerfile</c></description></item>
    ///   <item><description><c>./src/Dockerfile</c>  → context = <c>{packageDir}/src</c>, dockerfile = <c>Dockerfile</c></description></item>
    /// </list>
    /// </summary>
    private static (string ContextDir, string Dockerfile) ResolveBuildContext(string packageDir, string declared)
    {
        var rel = declared.Replace('\\', '/').TrimStart('.').TrimStart('/');
        var combined = Path.Combine(packageDir, rel);
        if (Directory.Exists(combined))
            return (Path.GetFullPath(combined), "Dockerfile");

        // Caller pointed at the Dockerfile itself.
        if (File.Exists(combined) &&
            string.Equals(Path.GetFileName(combined), "Dockerfile", StringComparison.OrdinalIgnoreCase))
        {
            return (Path.GetFullPath(Path.GetDirectoryName(combined)!), "Dockerfile");
        }

        // Plain "Dockerfile" at the package root.
        if (string.Equals(declared, "Dockerfile", StringComparison.OrdinalIgnoreCase))
            return (Path.GetFullPath(packageDir), "Dockerfile");

        // Fallback: treat as a directory under the package even if it
        // doesn't exist yet (caller will get a clear "Dockerfile not found").
        return (Path.GetFullPath(combined), "Dockerfile");
    }

    private static void ApplyYamlToChallenge(
        GameChallenge c, ChallengeYamlModel m, ChallengeType type, string? image,
        ChallengeImportOptions opts, string? packageDir = null)
    {
        c.Title = m.Name!;
        c.Category = ParseCategory(m.Category, packageDir);
        c.Content = string.IsNullOrEmpty(m.Author)
            ? (m.Description ?? string.Empty)
            : $"Author: **{m.Author}**\n\n{m.Description ?? string.Empty}";
        c.Hints = m.Hints;
        // 'value:' is intentionally ignored — points are admin-controlled.
        // New imports inherit the GameChallenge.OriginalScore default
        // (1000); existing rows keep whatever the admin already set.
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

        // 'visible:' is intentionally ignored — admin is the only one who
        // flips IsEnabled. New imports inherit whatever IsEnabled was
        // before the upsert (false for fresh challenges, untouched on
        // updates).
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

        Models.Data.LocalFile blob;
        if (File.Exists(absolute))
        {
            await using var fs = File.OpenRead(absolute);
            blob = await blobRepository.CreateOrUpdateBlobFromStream(Path.GetFileName(absolute), fs, token);
        }
        else if (Directory.Exists(absolute))
        {
            // gzcli/TCP1P convention: `provide: ./dist` ships a directory
            // of artifacts. Tar+gzip the directory contents into a single
            // blob so participants download one archive.
            var safe = NormalizeName(challenge.Title);
            var tarballName = $"{safe}.tar.gz";
            var tempPath = Path.Combine(Path.GetTempPath(), $"gzctf-provide-{Guid.NewGuid():N}.tar.gz");
            const long maxBytes = 256L * 1024 * 1024;
            try
            {
                long total = 0;
                foreach (var f in Directory.EnumerateFiles(absolute, "*", SearchOption.AllDirectories))
                {
                    total += new FileInfo(f).Length;
                    if (total > maxBytes)
                        throw new InvalidOperationException(
                            $"'provide' directory exceeds {maxBytes / (1024 * 1024)} MB (after tar+gzip cap).");
                }

                await using (var fs = File.Create(tempPath))
                await using (var gz = new GZipStream(fs, CompressionLevel.Fastest, leaveOpen: false))
                await using (var tar = new TarWriter(gz, leaveOpen: false))
                {
                    var dirCanonical = Path.GetFullPath(absolute) + Path.DirectorySeparatorChar;
                    foreach (var f in Directory.EnumerateFiles(absolute, "*", SearchOption.AllDirectories))
                    {
                        var name = Path.GetRelativePath(absolute, f).Replace('\\', '/');
                        if (name.StartsWith("..", StringComparison.Ordinal))
                            continue; // path-traversal guard belt-and-suspenders
                        var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                        {
                            DataStream = File.OpenRead(f)
                        };
                        await tar.WriteEntryAsync(entry, token);
                        entry.DataStream?.Dispose();
                    }
                }

                await using var rfs = File.OpenRead(tempPath);
                blob = await blobRepository.CreateOrUpdateBlobFromStream(tarballName, rfs, token);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { /* best effort */ }
            }
        }
        else
        {
            throw new FileNotFoundException($"'provide' file not found: {rel}");
        }

        await challengeRepository.UpdateAttachment(challenge,
            new AttachmentCreateModel
            {
                AttachmentType = FileType.Local,
                FileHash = blob.Hash
            }, token);
    }

    private static string NormalizeName(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        var safe = sb.ToString();
        return safe.Length > 0 ? safe : "attachment";
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
    /// Sniff the first two magic bytes of a seekable archive stream and
    /// dispatch to the right extractor. Supports gzipped tar (<c>1F 8B</c>)
    /// and classic zip (<c>50 4B</c>). Callers must pass a seekable stream.
    /// </summary>
    internal static async Task ExtractArchiveAsync(Stream archive, string workDir, CancellationToken token)
    {
        var magic = new byte[2];
        var read = await archive.ReadAsync(magic.AsMemory(0, 2), token);
        archive.Position = 0;
        if (read < 2)
            throw new InvalidOperationException("Archive too small.");

        if (magic[0] == 0x1F && magic[1] == 0x8B)
            await ExtractTarballStreamAsync(archive, workDir, token);
        else if (magic[0] == 0x50 && magic[1] == 0x4B)
            await ExtractZipStreamAsync(archive, workDir, token);
        else
            throw new InvalidOperationException("Unsupported archive format (expected .tar.gz or .zip).");
    }

    /// <summary>
    /// Extracts a gzipped tar stream into <paramref name="workDir"/>,
    /// rejecting any entry whose normalized path escapes the work dir.
    /// </summary>
    internal static async Task ExtractTarballStreamAsync(Stream tarGz, string workDir, CancellationToken token)
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

    /// <summary>
    /// Extracts a zip stream into <paramref name="workDir"/> with the same
    /// path-escape guard the tar extractor uses.
    /// </summary>
    internal static async Task ExtractZipStreamAsync(Stream zipStream, string workDir, CancellationToken token)
    {
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var canonical = Path.GetFullPath(workDir) + Path.DirectorySeparatorChar;

        foreach (var entry in zip.Entries)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.FullName)) continue;

            var dest = Path.GetFullPath(Path.Combine(workDir, entry.FullName));
            if (!dest.StartsWith(canonical, StringComparison.Ordinal))
                throw new InvalidOperationException($"Zip entry escapes work dir: {entry.FullName}");

            // Directory entries end with '/'.
            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            await using var src = entry.Open();
            await using var fs = File.Create(dest);
            await src.CopyToAsync(fs, token);
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

    private static ChallengeCategory ParseCategory(string? raw, string? packageDir = null)
    {
        if (!string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse<ChallengeCategory>(raw, ignoreCase: true, out var explicit_))
            return explicit_;

        // gzcli / TCP1P convention: challenge.yml omits 'category:' and the
        // category is the parent directory name (Crypto/<slug>/challenge.yml).
        // Walk up to three levels (challenge dir, parent, grandparent) looking
        // for a directory name that parses to a known category.
        if (!string.IsNullOrEmpty(packageDir))
        {
            var cur = new DirectoryInfo(packageDir).Parent;
            for (int i = 0; i < 3 && cur is not null; i++, cur = cur.Parent)
            {
                if (Enum.TryParse<ChallengeCategory>(cur.Name, ignoreCase: true, out var inferred))
                    return inferred;
            }
        }
        return ChallengeCategory.Misc;
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
