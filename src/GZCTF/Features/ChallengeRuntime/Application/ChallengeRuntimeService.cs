using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Features.ChallengeRuntime.Infrastructure;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using CanonicalChallenge = GZCTF.Features.ChallengeLibrary.Domain.Challenge;

namespace GZCTF.Features.ChallengeRuntime.Application;

public sealed record ChallengeInstanceResponse(
    Guid Id,
    ChallengeInstanceStatus Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? PublicIp,
    int? PublicPort,
    string? AttachmentFileName,
    string? AttachmentSha256);

public sealed class ChallengeRuntimeService(
    AppDbContext db,
    ILegacyContainerRuntimeAdapter containers)
{
    public async Task<UserChallengeInstance> GetOrCreateInstanceAsync(
        Guid userId, Guid challengeId, CancellationToken token = default)
    {
        var current = await db.UserChallengeInstances
            .SingleOrDefaultAsync(item => item.UserId == userId && item.ChallengeId == challengeId && item.IsActive,
                token);
        if (current is not null)
            return current;

        var instance = new UserChallengeInstance
        {
            UserId = userId,
            ChallengeId = challengeId,
            Status = ChallengeInstanceStatus.Pending,
            IsActive = true
        };
        db.UserChallengeInstances.Add(instance);
        try
        {
            await db.SaveChangesAsync(token);
            return instance;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.UserChallengeInstances.SingleAsync(item =>
                item.UserId == userId && item.ChallengeId == challengeId && item.IsActive, token);
        }
    }

    public async Task<UserChallengeInstance> StartAsync(
        Guid userId, Guid challengeId, CancellationToken token = default)
    {
        var instance = await GetOrCreateInstanceAsync(userId, challengeId, token);
        if (instance.Status == ChallengeInstanceStatus.Running)
            return instance;

        var challenge = await db.Challenges.Include(item => item.Flags)
            .SingleAsync(item => item.Id == challengeId, token);
        if (!challenge.Type.IsContainer())
            throw new InvalidOperationException("challenge.container_required");

        var settings = ReadContainerSettings(challenge.RuntimeConfigurationJson);
        var flag = ResolveFlag(challenge, userId, settings.FlagTemplate);
        var container = await containers.StartAsync(new CanonicalContainerRequest(
            userId,
            challengeId,
            settings.ContainerImage,
            settings.ExposedPort,
            settings.Cpu,
            settings.MemoryMb,
            settings.StorageMb,
            settings.NetworkMode,
            flag), token);
        if (container is null)
            throw new InvalidOperationException("challenge.container_unavailable");

        instance.ContainerId = container.Id;
        instance.AssignedFlag = flag;
        instance.Status = ChallengeInstanceStatus.Running;
        instance.StartedAtUtc ??= DateTimeOffset.UtcNow;
        instance.ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(2);
        await db.SaveChangesAsync(token);
        return instance;
    }

    public async Task<UserChallengeInstance> ExtendAsync(
        Guid userId, Guid challengeId, CancellationToken token = default)
    {
        var instance = await GetOwnedInstanceAsync(userId, challengeId, token);
        if (instance.Status != ChallengeInstanceStatus.Running)
            return instance;
        instance.ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(2);
        await db.SaveChangesAsync(token);
        return instance;
    }

    public async Task StopAsync(Guid userId, Guid challengeId, CancellationToken token = default)
    {
        var instance = await GetOwnedInstanceAsync(userId, challengeId, token);
        if (instance.Status == ChallengeInstanceStatus.Stopped)
            return;
        if (instance.ContainerId is Guid containerId)
        {
            var container = await db.Containers.SingleOrDefaultAsync(item => item.Id == containerId, token);
            if (container is not null)
                await containers.StopAsync(container, token);
        }

        instance.Status = ChallengeInstanceStatus.Stopped;
        instance.IsActive = false;
        instance.StoppedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
    }

    public async Task<UserChallengeInstance> GetOwnedInstanceAsync(
        Guid userId, Guid challengeId, CancellationToken token = default) =>
        await db.UserChallengeInstances.SingleAsync(item =>
            item.UserId == userId && item.ChallengeId == challengeId && item.IsActive, token);

    private static string? ResolveFlag(CanonicalChallenge challenge, Guid userId, string? template)
    {
        var staticFlag = challenge.Flags.FirstOrDefault(flag => flag.Kind == ChallengeFlagKind.Static)?.Value;
        if (challenge.Type == ChallengeType.StaticContainer)
            return staticFlag;
        return (template ?? string.Empty).Replace("{userId}", userId.ToString("N"), StringComparison.Ordinal);
    }

    private static ContainerSettings ReadContainerSettings(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new InvalidOperationException("challenge.container_configuration_missing");
        return JsonSerializer.Deserialize<ContainerSettings>(configuration,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("challenge.container_configuration_invalid");
    }

    private sealed record ContainerSettings(
        string ContainerImage,
        int ExposedPort,
        int Cpu,
        int MemoryMb,
        int StorageMb,
        string NetworkMode,
        string? FlagTemplate);
}
