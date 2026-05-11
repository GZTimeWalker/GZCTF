using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GZCTF.Test.UnitTests.Services;

public class ContainerAccessLoggerTest
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

    private static AppDbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static (ContainerAccessLogger logger, RecordingSuspicionService suspicion, AppDbContext db) Build(string name)
    {
        var db = NewDb(name);
        var suspicion = new RecordingSuspicionService();
        var options = Options.Create(new CheatDetectionConfig { LogContainerAccess = true });
        var logger = new ContainerAccessLogger(db, suspicion, options, NullLogger<ContainerAccessLogger>.Instance);
        return (logger, suspicion, db);
    }

    private static ContainerAccessContext Ctx(
        int ownerPid, Guid? userId = null, int? userPid = null, bool isAdmin = false) =>
        new(
            ContainerId: Guid.NewGuid(),
            ChallengeId: 1,
            ContainerOwnerParticipationId: ownerPid,
            GameId: 1,
            AccessingUserId: userId,
            AccessingUserName: userId is null ? null : "alice",
            AccessingParticipationId: userPid,
            RemoteIp: "1.2.3.4",
            UserAgent: "test",
            IsAdmin: isAdmin,
            ConnectedAtUtc: DateTimeOffset.UtcNow);

    [Fact]
    public async Task LogAccess_Anonymous_PersistsRow_NoSuspicion()
    {
        var (logger, suspicion, db) = Build(nameof(LogAccess_Anonymous_PersistsRow_NoSuspicion));

        await logger.LogAccess(Ctx(ownerPid: 10));

        var row = await db.ContainerAccessEvents.SingleAsync();
        Assert.Null(row.AccessingUserId);
        Assert.Empty(suspicion.Calls);
    }

    [Fact]
    public async Task LogAccess_SameTeam_PersistsRow_NoSuspicion()
    {
        var (logger, suspicion, db) = Build(nameof(LogAccess_SameTeam_PersistsRow_NoSuspicion));

        await logger.LogAccess(Ctx(ownerPid: 10, userId: Guid.NewGuid(), userPid: 10));

        Assert.Single(db.ContainerAccessEvents);
        Assert.Empty(suspicion.Calls);
    }

    [Fact]
    public async Task LogAccess_CrossTeamNonAdmin_PersistsRow_RaisesCrossTeam()
    {
        var (logger, suspicion, db) = Build(nameof(LogAccess_CrossTeamNonAdmin_PersistsRow_RaisesCrossTeam));

        var alienId = Guid.NewGuid();
        await logger.LogAccess(Ctx(ownerPid: 10, userId: alienId, userPid: 99, isAdmin: false));

        Assert.Single(db.ContainerAccessEvents);
        var call = Assert.Single(suspicion.Calls);
        Assert.Equal(10, call.Pid);
        Assert.Equal(SuspicionType.CrossTeamContainerAccess, call.Type);
        Assert.Equal(99, call.Related);
        Assert.Contains(alienId.ToString(), call.Details);
    }

    [Fact]
    public async Task LogAccess_CrossTeamAdmin_PersistsRow_NoSuspicion()
    {
        var (logger, suspicion, db) = Build(nameof(LogAccess_CrossTeamAdmin_PersistsRow_NoSuspicion));

        await logger.LogAccess(Ctx(ownerPid: 10, userId: Guid.NewGuid(), userPid: 99, isAdmin: true));

        Assert.Single(db.ContainerAccessEvents);
        Assert.Empty(suspicion.Calls);
    }

    [Fact]
    public async Task LogAccess_DisabledByConfig_DoesNothing()
    {
        var db = NewDb(nameof(LogAccess_DisabledByConfig_DoesNothing));
        var suspicion = new RecordingSuspicionService();
        var options = Options.Create(new CheatDetectionConfig { LogContainerAccess = false });
        var logger = new ContainerAccessLogger(db, suspicion, options, NullLogger<ContainerAccessLogger>.Instance);

        await logger.LogAccess(Ctx(ownerPid: 10, userId: Guid.NewGuid(), userPid: 99));

        Assert.Empty(db.ContainerAccessEvents);
        Assert.Empty(suspicion.Calls);
    }
}
