using System.Security.Cryptography;
using System.Text;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.Dashboard.Application;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeRuntime.Application;

public sealed record ChallengeSubmissionResult(
    bool Accepted,
    bool FirstSolve,
    ChallengeSolveMode? SolveMode,
    string? RejectionCode,
    Guid SubmissionId);

public sealed class ChallengeSubmissionService(
    AppDbContext db,
    ChallengeRuntimeService runtime,
    DynamicAttachmentAllocator attachments,
    DailySolveProjection dailyProjection)
{
    public async Task<ChallengeSubmissionResult> SubmitAsync(
        Guid userId, Guid challengeId, string submittedFlag, CancellationToken token = default)
    {
        var challenge = await db.Challenges.Include(item => item.Flags)
            .SingleAsync(item => item.Id == challengeId, token);
        var instance = await runtime.GetOrCreateInstanceAsync(userId, challengeId, token);
        var expected = await ResolveExpectedFlagAsync(challenge, instance, userId, token);
        var accepted = !string.IsNullOrEmpty(expected) &&
                       string.Equals(expected, submittedFlag, StringComparison.Ordinal);
        var submission = new ChallengeSubmission
        {
            UserId = userId,
            ChallengeId = challengeId,
            InstanceId = instance.Id,
            SubmittedFlagHash = Hash(submittedFlag),
            Accepted = accepted,
            RejectionCode = accepted ? null : "challenge.flag_incorrect"
        };
        db.ChallengeSubmissions.Add(submission);
        await db.SaveChangesAsync(token);

        if (!accepted)
            return new ChallengeSubmissionResult(false, false, null, submission.RejectionCode, submission.Id);

        var mode = await DetermineSolveModeAsync(userId, challengeId, token);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            var existing = await db.ChallengeProgress.AnyAsync(item =>
                item.UserId == userId && item.ChallengeId == challengeId, token);
            if (existing)
            {
                await transaction.CommitAsync(token);
                return new ChallengeSubmissionResult(true, false, null, null, submission.Id);
            }

            db.ChallengeProgress.Add(new ChallengeProgress
            {
                UserId = userId,
                ChallengeId = challengeId,
                SolveMode = mode
            });
            await dailyProjection.RecordFirstSolveAsync(userId, DateOnly.FromDateTime(DateTime.UtcNow), token);

            submission.FirstSolve = true;
            submission.SolveMode = mode;
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return new ChallengeSubmissionResult(true, true, mode, null, submission.Id);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(token);
            db.ChangeTracker.Clear();
            return new ChallengeSubmissionResult(true, false, null, null, submission.Id);
        }
    }

    private async Task<string?> ResolveExpectedFlagAsync(
        CanonicalChallenge challenge, UserChallengeInstance instance, Guid userId, CancellationToken token)
    {
        if (challenge.Type == ChallengeType.DynamicAttachment)
            return (await attachments.GetOrAllocateAsync(userId, challenge.Id, token)).Flag;
        if (challenge.Type == ChallengeType.DynamicContainer)
            return instance.AssignedFlag;
        return challenge.Flags.FirstOrDefault(item => item.Kind == ChallengeFlagKind.Static)?.Value;
    }

    private async Task<ChallengeSolveMode> DetermineSolveModeAsync(
        Guid userId, Guid challengeId, CancellationToken token)
    {
        if (await db.ChallengeHelpUsages.AnyAsync(item => item.UserId == userId &&
                                                           item.ChallengeId == challengeId && item.IsWriteup,
                token))
            return ChallengeSolveMode.AfterWriteup;
        return await db.ChallengeHelpUsages.AnyAsync(item => item.UserId == userId &&
                                                              item.ChallengeId == challengeId &&
                                                              item.HintId != null, token)
            ? ChallengeSolveMode.AfterHint
            : ChallengeSolveMode.Independent;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
