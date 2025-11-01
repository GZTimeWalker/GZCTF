using System.IO.Compression;
using GZCTF.Models.Transfer;
using GZCTF.Storage.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Game export service
/// </summary>
public class GameExportService(AppDbContext dbContext, IBlobStorage blobStorage)
{
    public record GameExportResult(Game Game, string ZipFilePath);

    /// <summary>
    /// Export game to ZIP file
    /// </summary>
    /// <param name="gameId">Game ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exported ZIP file path</returns>
    public async Task<GameExportResult?> ExportGameAsync(int gameId, CancellationToken ct = default)
    {
        // Load game data
        var game = await LoadGameDataAsync(gameId, ct);
        if (game is null)
            return null;

        // Create temporary working directory
        var workDir = Path.Combine(Path.GetTempPath(), $"gzctf-export-{gameId}-{Guid.NewGuid()}");
        Directory.CreateDirectory(workDir);

        try
        {
            // Convert to transfer models
            var transferGame = game.ToTransfer();
            var transferChallenges = game.Challenges.Select(c => c.ToTransfer()).ToList();

            // Write transfer files
            await WriteTransferFilesAsync(workDir, transferGame, transferChallenges, ct);

            // Copy attachment files (including game poster)
            await CopyAttachmentsAsync(game, workDir, ct);

            // Calculate statistics and verify file integrity
            var statistics = await CalculateStatisticsAsync(workDir, transferGame, transferChallenges, ct);

            // Create manifest with statistics
            var manifest = new TransferManifest
            {
                Version = "1.0",
                Format = "GZCTF-GAME",
                ExportedAt = DateTimeOffset.UtcNow,
                ExporterVersion = TransferHelper.GetExporterVersion(),
                Game = new GameInfoSection { Id = game.Id, Title = game.Title },
                Statistics = statistics
            };

            // Write manifest
            var manifestPath = Path.Combine(workDir, "manifest.json");
            var manifestContent = TransferHelper.ToJson(manifest);
            await File.WriteAllTextAsync(manifestPath, manifestContent, ct);

            // Create ZIP package
            var zipPath = Path.Combine(Path.GetTempPath(),
                $"game-{gameId}-export-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");

            ZipFile.CreateFromDirectory(workDir, zipPath);
            return new(game, zipPath);
        }
        finally
        {
            // Clean up temporary directory
            if (Directory.Exists(workDir))
                Directory.Delete(workDir, true);
        }
    }

    /// <summary>
    /// Load game data required for export (sliced queries to avoid cartesian explosion)
    /// 1. Load Game
    /// 2. Load Divisions with their ChallengeConfigs
    /// 3. Load Challenges with Attachment.LocalFile
    ///    - For static challenges: additionally load Flags.Attachment.LocalFile
    ///    - For dynamic challenges: do not load Flags
    /// </summary>
    private async Task<Game?> LoadGameDataAsync(int gameId, CancellationToken ct)
    {
        // 1) Basic Game (without any navigations)
        var game = await dbContext.Games
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(g => g.Id == gameId)
            .SingleOrDefaultAsync(ct);

        if (game is null)
            return null;

        // 2) Divisions with their ChallengeConfigs
        var divisions = await dbContext.Divisions
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(d => d.GameId == gameId)
            .Include(d => d.ChallengeConfigs)
            .ToHashSetAsync(ct);

        // 3) Challenges (including Challenge.Attachment.LocalFile)
        var challenges = await dbContext.GameChallenges
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(c => c.GameId == gameId)
            .Include(c => c.Attachment)
            .ThenInclude(a => a!.LocalFile)
            .ToHashSetAsync(ct);

        // 3.1) Load Flags and their Attachment.LocalFile only for static challenges
        var staticIds = challenges
            .Where(c => c.Type.IsStatic())
            .Select(c => c.Id)
            .ToList();

        if (staticIds.Count > 0)
        {
            var flags = await dbContext.FlagContexts
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(f => f.ChallengeId != null && staticIds.Contains(f.ChallengeId.Value))
                .Include(f => f.Attachment)
                .ThenInclude(a => a!.LocalFile)
                .ToListAsync(ct);

            var flagsByChallenge = flags
                .GroupBy(f => f.ChallengeId!.Value)
                .ToDictionary(gp => gp.Key, gp => gp.ToList());

            foreach (var ch in challenges)
                if (flagsByChallenge.TryGetValue(ch.Id, out var list))
                    ch.Flags = list;
        }

        // Assemble sliced data back into Game
        game.Divisions = divisions;
        game.Challenges = challenges;

        return game;
    }

    /// <summary>
    /// Write transfer format files to working directory
    /// </summary>
    private static async Task WriteTransferFilesAsync(
        string workDir,
        TransferGame transferGame,
        List<TransferChallenge> transferChallenges,
        CancellationToken ct)
    {
        // Write game.json
        var gamePath = Path.Combine(workDir, "game.json");
        var gameToml = TransferHelper.ToJson(transferGame);
        await File.WriteAllTextAsync(gamePath, gameToml, ct);

        // Create challenges directory and write challenge files
        var challengesDir = Path.Combine(workDir, "challenges");
        Directory.CreateDirectory(challengesDir);
        foreach (var challenge in transferChallenges)
        {
            var challengePath = Path.Combine(challengesDir, $"challenge-{challenge.Id:D}.json");
            var challengeToml = TransferHelper.ToJson(challenge);
            await File.WriteAllTextAsync(challengePath, challengeToml, ct);
        }
    }

    /// <summary>
    /// Copy all attachment files to working directory
    /// </summary>
    private async Task CopyAttachmentsAsync(Game game, string workDir,
        CancellationToken ct)
    {
        var filesDir = Path.Combine(workDir, "files");
        Directory.CreateDirectory(filesDir);

        var copiedHashes = new HashSet<string>(); // Track copied files to avoid duplicates

        // Copy game poster if exists
        if (!string.IsNullOrWhiteSpace(game.PosterHash))
            await CopyFileByHashAsync(game.PosterHash, filesDir, copiedHashes, ct);

        foreach (var challenge in game.Challenges)
        {
            // Copy challenge attachment
            if (challenge.Attachment is not null)
                await CopyAttachmentAsync(challenge.Attachment, filesDir, copiedHashes, ct);

            // Copy flag attachments
            foreach (var flag in challenge.Flags)
            {
                if (flag.Attachment is not null)
                    await CopyAttachmentAsync(flag.Attachment, filesDir, copiedHashes, ct);
            }
        }
    }

    /// <summary>
    /// Copy a single attachment file from blob storage
    /// </summary>
    private ValueTask<bool> CopyAttachmentAsync(Attachment attachment, string filesDir, HashSet<string> copiedHashes,
        CancellationToken ct) =>
        attachment.LocalFile is null
            ? ValueTask.FromResult(true)
            : CopyFileByHashAsync(attachment.LocalFile.Hash, filesDir, copiedHashes, ct);

    /// <summary>
    /// Copy a file by hash from blob storage
    /// </summary>
    private async ValueTask<bool> CopyFileByHashAsync(string hash, string filesDir, HashSet<string> copiedHashes,
        CancellationToken ct)
    {
        // Skip if already copied (handle duplicate attachments)
        if (!copiedHashes.Add(hash))
            return true;

        // check if hash is valid
        if (hash.Length != 64)
            return false;

        var filePath = StoragePath.Combine(PathHelper.Uploads, hash[..2], hash[2..4], hash);

        // check if blob exists
        if (!await blobStorage.ExistsAsync(filePath, ct))
            return false;

        var targetPath = Path.Combine(filesDir, hash);
        await using var stream = await blobStorage.OpenReadAsync(filePath, ct);
        await using var fileStream = File.Create(targetPath);
        await stream.CopyToAsync(fileStream, ct);

        return true;
    }

    /// <summary>
    /// Calculate export statistics with streaming hash verification
    /// Verifies file integrity by computing SHA256 hash of each file
    /// and matching it against the filename
    /// </summary>
    private async Task<StatisticsSection> CalculateStatisticsAsync(
        string workDir,
        TransferGame transferGame,
        List<TransferChallenge> transferChallenges,
        CancellationToken ct)
    {
        var filesDir = Path.Combine(workDir, "files");

        // Count total flags
        var totalFlags = transferChallenges.Sum(c =>
            (c.Flags.Template is not null ? 1 : 0) + (c.Flags.Static?.Count ?? 0));

        var statistics = new StatisticsSection
        {
            ChallengeCount = transferChallenges.Count,
            DivisionCount = transferGame.Divisions.Count,
            TotalFlags = totalFlags,
            TotalFiles = 0,
            TotalFileSize = 0
        };

        if (!Directory.Exists(filesDir))
            return statistics;

        // Count total files and calculate total size with hash verification
        var files = Directory.GetFiles(filesDir);
        statistics.TotalFiles = files.Length;

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            statistics.TotalFileSize += fileInfo.Length;

            // Verify file integrity using streaming hash computation
            var expectedHash = Path.GetFileName(file);
            var computedHash = await TransferHelper.ComputeFileHashAsync(file, ct);

            if (!computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"File integrity check failed for '{expectedHash}': " +
                    $"computed hash '{computedHash}' does not match filename");
            }
        }

        return statistics;
    }
}
