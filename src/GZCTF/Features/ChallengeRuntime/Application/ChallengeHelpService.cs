using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeRuntime.Application;

public sealed record ChallengeHintResponse(Guid HintId, int SortOrder, string Locale, string Content);

public sealed record ChallengeWriteupResponse(string Locale, string Content, DateTimeOffset FirstViewedAtUtc);

public sealed class ChallengeHelpService(AppDbContext db)
{
    public async Task<ChallengeHintResponse?> RevealNextHintAsync(
        Guid userId, Guid challengeId, string? locale, CancellationToken token = default)
    {
        var challenge = await db.Challenges.Include(item => item.Hints)
            .SingleOrDefaultAsync(item => item.Id == challengeId, token);
        if (challenge is null)
            return null;

        var localizedHints = LocalizeHints(challenge, locale);
        var viewed = await db.ChallengeHelpUsages
            .Where(item => item.UserId == userId && item.ChallengeId == challengeId && item.HintId != null)
            .Select(item => item.HintId!.Value)
            .ToHashSetAsync(token);
        var next = localizedHints.FirstOrDefault(item => !viewed.Contains(item.Id));
        if (next is null)
            return null;

        db.ChallengeHelpUsages.Add(new ChallengeHelpUsage
        {
            UserId = userId,
            ChallengeId = challengeId,
            HintId = next.Id,
            IsWriteup = false
        });
        await db.SaveChangesAsync(token);
        return new ChallengeHintResponse(next.Id, next.SortOrder, next.Locale, next.Content);
    }

    public async Task<ChallengeWriteupResponse?> RevealWriteupAsync(
        Guid userId, Guid challengeId, string? locale, CancellationToken token = default)
    {
        var challenge = await db.Challenges.Include(item => item.Writeups)
            .SingleOrDefaultAsync(item => item.Id == challengeId, token);
        if (challenge is null)
            return null;

        var writeup = PickWriteup(challenge, locale);
        if (writeup is null)
            return null;
        var usage = await db.ChallengeHelpUsages.SingleOrDefaultAsync(item =>
            item.UserId == userId && item.ChallengeId == challengeId && item.IsWriteup, token);
        if (usage is null)
        {
            usage = new ChallengeHelpUsage
            {
                UserId = userId,
                ChallengeId = challengeId,
                IsWriteup = true
            };
            db.ChallengeHelpUsages.Add(usage);
            await db.SaveChangesAsync(token);
        }

        return new ChallengeWriteupResponse(writeup.Locale, writeup.Content, usage.ViewedAtUtc);
    }

    private static IReadOnlyList<ChallengeHint> LocalizeHints(CanonicalChallenge challenge, string? locale)
    {
        var requested = challenge.Hints.Where(item =>
                string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.SortOrder)
            .ToArray();
        if (requested.Length > 0)
            return requested;
        return challenge.Hints.Where(item => string.Equals(item.Locale, "en", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.SortOrder)
            .ToArray();
    }

    private static ChallengeWriteup? PickWriteup(CanonicalChallenge challenge, string? locale) =>
        challenge.Writeups.FirstOrDefault(item =>
            string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase)) ??
        challenge.Writeups.FirstOrDefault(item => string.Equals(item.Locale, "en",
            StringComparison.OrdinalIgnoreCase));
}
