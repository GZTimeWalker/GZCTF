using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GZCTF.Test.UnitTests.Services;

public class ContainerAccessSubmissionDetectorTest
{
    private sealed class RecordingSuspicionService : ISuspicionService
    {
        public List<(int Pid, string Type, string Details, int? Related)> Calls { get; } = new();

        public Task AddSuspicion(Participation participation, string ruleCode, string details,
            int? relatedParticipationId = null, CancellationToken token = default)
        {
            Calls.Add((participation.Id, ruleCode, details, relatedParticipationId));
            return Task.CompletedTask;
        }

        public Task<int> GetScore(Participation participation, CancellationToken token = default) =>
            Task.FromResult(0);
    }

    private sealed class StubIpAttribution : IIpAttributionHelper
    {
        public IPAddress? Result { get; set; }

        public Task<IPAddress?> ResolveUserIpAt(AppDbContext db, string userName, DateTimeOffset center,
            TimeSpan window, CancellationToken token) => Task.FromResult(Result);
    }

    private static AppDbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static Submission MakeSubmission(Guid userId, DateTimeOffset submitAt, string? userName = "alice") => new()
    {
        Id = 1,
        ChallengeId = 1,
        ParticipationId = 10,
        GameId = 1,
        TeamId = 100,
        UserId = userId,
        User = new UserInfo { Id = userId, UserName = userName, Email = "a@a" },
        SubmitTimeUtc = submitAt,
        Answer = "flag{x}",
        Status = AnswerResult.Accepted,
    };

    private static ContainerAccessEvent Access(int cid, int pid, Guid? userId, string ip, DateTimeOffset t) => new()
    {
        ChallengeId = cid,
        ContainerOwnerParticipationId = pid,
        GameId = 1,
        ContainerId = Guid.NewGuid(),
        AccessingUserId = userId,
        AccessingParticipationId = pid,
        RemoteIp = ip,
        ConnectedAtUtc = t,
    };

    private static (ContainerAccessSubmissionDetector det, RecordingSuspicionService sus, StubIpAttribution ip, AppDbContext db)
        Build(string name, CheatDetectionConfig? cfg = null)
    {
        var db = NewDb(name);
        var sus = new RecordingSuspicionService();
        var ip = new StubIpAttribution();
        var options = Options.Create(cfg ?? new CheatDetectionConfig());
        var det = new ContainerAccessSubmissionDetector(db, sus, ip, options, NullLogger<ContainerAccessSubmissionDetector>.Instance);
        return (det, sus, ip, db);
    }

    [Fact]
    public async Task RunChecks_PlatformProxyDisabled_RaisesNothing()
    {
        var (det, sus, _, _) = Build(nameof(RunChecks_PlatformProxyDisabled_RaisesNothing));
        var sub = MakeSubmission(Guid.NewGuid(), DateTimeOffset.UtcNow);

        await det.RunChecks(sub, platformProxyEnabled: false);

        Assert.Empty(sus.Calls);
    }

    [Fact]
    public async Task RunChecks_NoSubmitterUser_RaisesNothing()
    {
        var (det, sus, _, _) = Build(nameof(RunChecks_NoSubmitterUser_RaisesNothing));
        var sub = MakeSubmission(Guid.Empty, DateTimeOffset.UtcNow);
        sub.UserId = null;

        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.Empty(sus.Calls);
    }

    [Fact]
    public async Task RunChecks_NoAccessEventsAtAll_RaisesNothing()
    {
        var (det, sus, _, _) = Build(nameof(RunChecks_NoAccessEventsAtAll_RaisesNothing));
        var sub = MakeSubmission(Guid.NewGuid(), DateTimeOffset.UtcNow);

        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.Empty(sus.Calls);
    }

    [Fact]
    public async Task RunChecks_DelayedSubmission_Fires()
    {
        var (det, sus, _, db) = Build(nameof(RunChecks_DelayedSubmission_Fires));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        var accessAt = submitAt.AddMinutes(-90);
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", accessAt));
        await db.SaveChangesAsync();

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        var delayed = sus.Calls.Where(c => c.Type == SuspicionType.DelayedSolveSubmission).ToList();
        Assert.Single(delayed);
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.InstantSubmitAfterAccess);
    }

    [Fact]
    public async Task RunChecks_InstantSubmission_Fires()
    {
        var (det, sus, _, db) = Build(nameof(RunChecks_InstantSubmission_Fires));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        var accessAt = submitAt.AddSeconds(-1);
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", accessAt));
        await db.SaveChangesAsync();

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        var instant = sus.Calls.Where(c => c.Type == SuspicionType.InstantSubmitAfterAccess).ToList();
        Assert.Single(instant);
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.DelayedSolveSubmission);
    }

    [Fact]
    public async Task RunChecks_GoldilocksLatency_FiresNeitherTimingSignal()
    {
        var (det, sus, _, db) = Build(nameof(RunChecks_GoldilocksLatency_FiresNeitherTimingSignal));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        var accessAt = submitAt.AddMinutes(-15); // between 3s and 60min
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", accessAt));
        await db.SaveChangesAsync();

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.DelayedSolveSubmission);
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.InstantSubmitAfterAccess);
    }

    [Fact]
    public async Task RunChecks_ClockSkewNegativeLatency_DoesNotFireInstantSubmit()
    {
        var (det, sus, _, db) = Build(nameof(RunChecks_ClockSkewNegativeLatency_DoesNotFireInstantSubmit));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        // Access "after" submit by 2s — pathological clock skew. Clamped to zero → falls under InstantThreshold (3s).
        var accessAt = submitAt.AddSeconds(2);
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", accessAt));
        await db.SaveChangesAsync();

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        // The clamp-to-zero behavior means clock skew is treated as instant-submit. That's intentional
        // (we'd rather over-fire instant than silently miss it on clock skew). Asserting current behavior.
        // Note: events with ConnectedAtUtc > SubmitTimeUtc are filtered out at the EF query level,
        // so this row should not be loaded at all → no signal.
        Assert.Empty(sus.Calls);
    }

    [Fact]
    public async Task RunChecks_TeammateAccessedSubmitterDidNot_FiresSubmitterNeverAccessed()
    {
        var (det, sus, _, db) = Build(nameof(RunChecks_TeammateAccessedSubmitterDidNot_FiresSubmitterNeverAccessed));
        var submitterId = Guid.NewGuid();
        var teammateId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;

        db.ContainerAccessEvents.Add(Access(1, 10, teammateId, "9.9.9.9", submitAt.AddMinutes(-30)));
        await db.SaveChangesAsync();

        var sub = MakeSubmission(submitterId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.Contains(sus.Calls, c => c.Type == SuspicionType.SubmitterNeverAccessedContainer);
        // Timing signals don't fire because submitter has no access events.
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.DelayedSolveSubmission);
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.InstantSubmitAfterAccess);
        // IP mismatch doesn't fire because submitter has no access events to compare against.
        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.AccessIpMismatchAtSubmission);
    }

    [Fact]
    public async Task RunChecks_IpMismatch_Fires()
    {
        var (det, sus, ip, db) = Build(nameof(RunChecks_IpMismatch_Fires));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", submitAt.AddMinutes(-15)));
        await db.SaveChangesAsync();

        ip.Result = IPAddress.Parse("2.2.2.2");

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.Contains(sus.Calls, c => c.Type == SuspicionType.AccessIpMismatchAtSubmission);
    }

    [Fact]
    public async Task RunChecks_IpMatch_DoesNotFireMismatch()
    {
        var (det, sus, ip, db) = Build(nameof(RunChecks_IpMatch_DoesNotFireMismatch));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", submitAt.AddMinutes(-15)));
        await db.SaveChangesAsync();

        ip.Result = IPAddress.Parse("1.1.1.1");

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.AccessIpMismatchAtSubmission);
    }

    [Fact]
    public async Task RunChecks_IpUnresolved_DoesNotFireMismatch()
    {
        var (det, sus, ip, db) = Build(nameof(RunChecks_IpUnresolved_DoesNotFireMismatch));
        var userId = Guid.NewGuid();
        var submitAt = DateTimeOffset.UtcNow;
        db.ContainerAccessEvents.Add(Access(1, 10, userId, "1.1.1.1", submitAt.AddMinutes(-15)));
        await db.SaveChangesAsync();

        ip.Result = null; // helper failed to resolve

        var sub = MakeSubmission(userId, submitAt);
        await det.RunChecks(sub, platformProxyEnabled: true);

        Assert.DoesNotContain(sus.Calls, c => c.Type == SuspicionType.AccessIpMismatchAtSubmission);
    }
}
