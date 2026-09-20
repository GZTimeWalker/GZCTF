using System.Text.Json;
using GZCTF.Features.Imports.Application;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Imports.Infrastructure;

public sealed class LegacyDatabaseSource(AppDbContext db)
{
    public async Task<CanonicalChallengeImportBatch> ReadAsync(CancellationToken token = default)
    {
        var games = await db.Games.AsNoTracking()
            .Include(game => game.Challenges)
                .ThenInclude(challenge => challenge.Flags)
                    .ThenInclude(flag => flag.Attachment)
                        .ThenInclude(attachment => attachment!.LocalFile)
            .OrderBy(game => game.Id)
            .ToListAsync(token);
        var paths = games.Select(game => new CanonicalPathImport(
            "legacy-db",
            game.Id.ToString(),
            $"legacy-game-{game.Id}",
            game.Title,
            game.Summary,
            game.Challenges.GroupBy(challenge => challenge.Category)
                .OrderBy(group => group.Key)
                .Select((group, index) => new CanonicalModuleImport(
                    $"{game.Id}:{group.Key}",
                    group.Key.ToString(),
                    $"Imported {group.Key}",
                    index,
                    group.OrderBy(challenge => challenge.Id)
                        .Select((challenge, order) => ToImport(game, challenge, order)).ToArray()))
                .ToArray())).ToArray();

        var fingerprint = "legacy-database-v1";
        return new CanonicalChallengeImportBatch("legacy-database", fingerprint, paths, []);
    }

    private static CanonicalChallengeImport ToImport(Game game, GameChallenge challenge, int order)
    {
        var flags = challenge.Flags ?? [];
        var flag = flags.FirstOrDefault()?.Flag;
        var attachments = flags.Where(item => item.Attachment?.LocalFile is not null)
            .Select(item => new ImportAttachment(
                item.Attachment!.LocalFile!.Name,
                item.Attachment.LocalFile.Hash,
                item.Attachment.LocalFile.Url(),
                item.Flag)).ToArray();
        var container = challenge.Type.IsContainer()
            ? new ImportContainerConfiguration(
                challenge.ContainerImage ?? string.Empty,
                challenge.ExposePort ?? 0,
                challenge.CPUCount ?? 0,
                challenge.MemoryLimit ?? 0,
                challenge.StorageLimit ?? 0,
                (challenge.NetworkMode ?? NetworkMode.Open).ToString())
            : null;
        var metadata = JsonSerializer.Serialize(new
        {
            gameId = game.Id,
            legacyScore = challenge.OriginalScore,
            legacyDifficulty = challenge.Difficulty,
            category = challenge.Category.ToString()
        });
        return new CanonicalChallengeImport(
            "legacy-db",
            $"{game.Id}:{challenge.Id}",
            challenge.Type,
            [new ImportLocalizedText("en", challenge.Title, string.Empty, challenge.Content)],
            (challenge.Hints ?? []).Select((content, index) => new ImportHint("en", index, content)).ToArray(),
            challenge.Type.IsDynamic() ? null : flag ?? "flag{legacy-flag-recovery-required}",
            challenge.Type == ChallengeType.DynamicContainer ? challenge.FlagTemplate : null,
            attachments,
            container,
            metadata,
            null,
            order);
    }
}
