using System.Data;
using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeRuntime.Application;

public sealed record AttachmentAssignment(
    string FileName,
    string Sha256,
    string Flag,
    string? StorageKey = null)
{
    public string EffectiveStorageKey => StorageKey ?? FileName;
}

public sealed class DynamicAttachmentExhaustedException : Exception
{
    public DynamicAttachmentExhaustedException()
        : base("challenge.dynamic_attachment_exhausted")
    {
    }
}

public sealed class DynamicAttachmentAllocator(AppDbContext db)
{
    public async Task<AttachmentAssignment> GetOrAllocateAsync(
        Guid userId, Guid challengeId, CancellationToken token = default)
    {
        var current = await db.UserChallengeInstances
            .SingleOrDefaultAsync(instance => instance.UserId == userId &&
                                              instance.ChallengeId == challengeId && instance.IsActive, token)
                      ?? throw new InvalidOperationException("An active challenge instance is required.");

        var challenge = await db.Challenges
            .Include(item => item.Flags)
            .SingleAsync(item => item.Id == challengeId, token);
        var candidates = ReadCandidates(challenge);

        if (!string.IsNullOrWhiteSpace(current.AssignedAttachmentKey) &&
            !string.IsNullOrWhiteSpace(current.AssignedAttachmentSha256) &&
            !string.IsNullOrWhiteSpace(current.AssignedFlag))
            return candidates.FirstOrDefault(item =>
                       item.EffectiveStorageKey == current.AssignedAttachmentKey &&
                       item.Sha256 == current.AssignedAttachmentSha256)
                   ?? new AttachmentAssignment(current.AssignedAttachmentKey,
                       current.AssignedAttachmentSha256, current.AssignedFlag);
        if (candidates.Count == 0)
            throw new InvalidOperationException("challenge.attachment_pool_missing");

        if (challenge.Type == ChallengeType.StaticAttachment)
            return await AssignAsync(current, candidates[0], token);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, token);
            try
            {
                var instance = await db.UserChallengeInstances
                    .SingleAsync(item => item.Id == current.Id, token);
                if (!string.IsNullOrWhiteSpace(instance.AssignedAttachmentKey) &&
                    !string.IsNullOrWhiteSpace(instance.AssignedAttachmentSha256) &&
                    !string.IsNullOrWhiteSpace(instance.AssignedFlag))
                {
                    await transaction.CommitAsync(token);
                    return new AttachmentAssignment(instance.AssignedAttachmentKey,
                        instance.AssignedAttachmentSha256, instance.AssignedFlag);
                }

                var used = await db.UserChallengeInstances
                    .Where(instance => instance.ChallengeId == challengeId &&
                                       instance.IsActive && instance.AssignedAttachmentSha256 != null)
                    .Select(instance => instance.AssignedAttachmentSha256!)
                    .ToHashSetAsync(token);
                var candidate = candidates.FirstOrDefault(item => !used.Contains(item.Sha256));
                if (candidate is null)
                    throw new DynamicAttachmentExhaustedException();

                instance.AssignedAttachmentKey = candidate.EffectiveStorageKey;
                instance.AssignedAttachmentSha256 = candidate.Sha256;
                instance.AssignedFlag = candidate.Flag;
                await db.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return candidate;
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                await transaction.RollbackAsync(token);
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Attachment allocation could not be serialized.");
    }

    private async Task<AttachmentAssignment> AssignAsync(
        UserChallengeInstance instance, AttachmentAssignment assignment, CancellationToken token)
    {
        instance.AssignedAttachmentKey = assignment.EffectiveStorageKey;
        instance.AssignedAttachmentSha256 = assignment.Sha256;
        instance.AssignedFlag = assignment.Flag;
        await db.SaveChangesAsync(token);
        return assignment;
    }

    private static IReadOnlyList<AttachmentAssignment> ReadCandidates(CanonicalChallenge challenge)
    {
        var metadata = challenge.Flags
            .FirstOrDefault(flag => flag.Kind == ChallengeFlagKind.DynamicAttachment)?.MetadataJson;
        if (string.IsNullOrWhiteSpace(metadata) && !string.IsNullOrWhiteSpace(challenge.RuntimeConfigurationJson))
        {
            using var document = JsonDocument.Parse(challenge.RuntimeConfigurationJson);
            if (document.RootElement.TryGetProperty("Attachments", out var attachments))
                metadata = attachments.GetRawText();
        }
        if (string.IsNullOrWhiteSpace(metadata))
            throw new InvalidOperationException("challenge.attachment_pool_missing");

        var candidates = JsonSerializer.Deserialize<List<AttachmentCandidate>>(metadata,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return candidates?.Select(item => new AttachmentAssignment(
                item.FileName,
                item.Sha256,
                item.Flag,
                AttachmentStorageKey.Normalize(item.StorageKey, item.FileName)))
            .Where(item => !string.IsNullOrWhiteSpace(item.FileName) &&
                          !string.IsNullOrWhiteSpace(item.Sha256) &&
                          !string.IsNullOrWhiteSpace(item.Flag))
            .ToArray()
            ?? throw new InvalidOperationException("challenge.attachment_pool_invalid");
    }

    private sealed record AttachmentCandidate(string FileName, string Sha256, string Flag, string? StorageKey);

}
