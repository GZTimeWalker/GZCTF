using System.Buffers.Binary;
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
using Microsoft.EntityFrameworkCore.Storage;
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
    DailySolveProjection dailyProjection,
    DashboardDeltaPublisher dashboardDeltas)
{
    public async Task<ChallengeSubmissionResult> SubmitAsync(
        Guid userId, Guid challengeId, string submittedFlag, CancellationToken token = default)
    {
        var challenge = await db.Challenges.Include(item => item.Flags)
            .SingleAsync(item => item.Id == challengeId, token);
        IDbContextTransaction? limitTransaction = null;
        try
        {
            if (challenge.SubmissionLimit > 0)
            {
                limitTransaction = await db.Database.BeginTransactionAsync(token);
                var lockInput = userId.ToByteArray().Concat(challengeId.ToByteArray()).ToArray();
                var lockKey = BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(lockInput));
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({lockKey})", token);
                if (await db.ChallengeSubmissions.CountAsync(item =>
                        item.UserId == userId && item.ChallengeId == challengeId, token) >= challenge.SubmissionLimit)
                {
                    await limitTransaction.CommitAsync(token);
                    return new ChallengeSubmissionResult(false, false, null,
                        "challenge.submission_limit_exhausted", Guid.Empty);
                }
            }

            var instance = await runtime.GetOrCreateInstanceAsync(userId, challengeId, token);
            var accepted = challenge.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer
                ? challenge.Flags.Where(item => item.Kind == ChallengeFlagKind.Static)
                    .Aggregate(false, (match, item) => MatchesFlag(item.Value, submittedFlag) | match)
                : MatchesFlag(await ResolveExpectedFlagAsync(challenge, instance, userId, token), submittedFlag);
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
            {
                if (limitTransaction is not null) await limitTransaction.CommitAsync(token);
                return new ChallengeSubmissionResult(false, false, null, submission.RejectionCode, submission.Id);
            }

            // The limit lock only needs to cover count + insert. Commit the submission
            // before the solve-projection transaction so a later progress conflict
            // cannot erase a recorded attempt.
            if (limitTransaction is not null)
            {
                await limitTransaction.CommitAsync(token);
                await limitTransaction.DisposeAsync();
                limitTransaction = null;
            }

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
                await dashboardDeltas.PublishFirstSolveAsync(userId, token);
                return new ChallengeSubmissionResult(true, true, mode, null, submission.Id);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(token);
                db.ChangeTracker.Clear();
                return new ChallengeSubmissionResult(true, false, null, null, submission.Id);
            }
        }
        finally
        {
            if (limitTransaction is not null) await limitTransaction.DisposeAsync();
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

    private static bool MatchesFlag(string? expected, string submitted)
    {
        if (string.IsNullOrEmpty(expected)) return false;
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(submitted)));
    }
}
