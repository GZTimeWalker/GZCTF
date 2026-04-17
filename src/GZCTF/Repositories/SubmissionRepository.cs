using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SubmissionRepository(
    IHubContext<MonitorHub, IMonitorClient> hub,
    IHubContext<AttackHub, IAttackClient> attackHub,
    AppDbContext context) : RepositoryBase(context), ISubmissionRepository
{
    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await Context.AddAsync(submission, token);
        await Context.SaveChangesAsync(token);

        return submission;
    }

    public Task<Submission?> GetSubmission(int gameId, int challengeId, Guid userId, int submitId,
        CancellationToken token = default)
        => Context.Submissions.IgnoreAutoIncludes().Where(s =>
                s.Id == submitId && s.UserId == userId && s.GameId == gameId && s.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public Task<int> CountSubmissions(int participationId, int challengeId, CancellationToken token = default) =>
        Context.Submissions.CountAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == challengeId, token);

    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default) =>
        Context.Submissions.Where(s => s.Status == AnswerResult.FlagSubmitted)
            .AsNoTracking().Include(e => e.Game).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Game game, AnswerResult? type = null, int count = 100, int skip = 0,
        string? search = null, CancellationToken token = default)
    {
        var query = GetSubmissionsByType(type).Where(s => s.Game == game);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                (s.Team != null && EF.Functions.Like(s.Team.Name, $"%{search}%")) ||
                (s.User != null && EF.Functions.Like(s.User.UserName, $"%{search}%")) ||
                (s.GameChallenge != null && EF.Functions.Like(s.GameChallenge.Title, $"%{search}%")) ||
                EF.Functions.Like(s.Answer, $"%{search}%"));
        }

        return query.TakeAllIfZero(count, skip).ToArrayAsync(token);
    }

    public Task<Submission[]> GetSubmissions(GameChallenge challenge, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default) =>
        GetSubmissionsByType(type).Where(s => s.GameChallenge == challenge).TakeAllIfZero(count, skip)
            .ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Participation team, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default) =>
        GetSubmissionsByType(type).Where(s => s.TeamId == team.TeamId).TakeAllIfZero(count, skip)
            .ToArrayAsync(token);

    public Task SendSubmission(Submission submission) =>
        // Existing behavior: broadcast to the admin monitor hub only.
        // The public AttackHub is notified via SendAttackEvent, which is
        // invoked separately from FlagChecker once the precise SubmissionType
        // (Normal/FirstBlood/SecondBlood/ThirdBlood/Unaccepted) is known.
        hub.Clients.Group($"Game_{submission.GameId}")
            .ReceivedSubmissions(submission);

    public Task SendAttackEvent(Submission submission, SubmissionType type) =>
        SendAttackEventInternal(submission, type);

    private async Task SendAttackEventInternal(Submission submission, SubmissionType type)
    {
        // Prefer navigation-loaded data; fall back to projection if missing.
        var teamName = submission.Team?.Name ?? submission.TeamName;
        var teamAvatar = submission.Team?.AvatarUrl;
        var challengeTitle = submission.GameChallenge?.Title ?? submission.ChallengeName;
        var category = submission.GameChallenge?.Category ?? ChallengeCategory.Misc;

        if (string.IsNullOrEmpty(teamName) || string.IsNullOrEmpty(challengeTitle))
        {
            // Fetch join data if not already loaded.
            var projection = await Context.Submissions
                .AsNoTracking()
                .Where(s => s.Id == submission.Id)
                .Select(s => new
                {
                    TeamName = s.Team != null ? s.Team.Name : string.Empty,
                    AvatarHash = s.Team != null ? s.Team.AvatarHash : null,
                    ChallengeTitle = s.GameChallenge != null ? s.GameChallenge.Title : string.Empty,
                    Category = s.GameChallenge != null ? s.GameChallenge.Category : ChallengeCategory.Misc
                })
                .SingleOrDefaultAsync();

            if (projection is not null)
            {
                teamName = projection.TeamName;
                teamAvatar = projection.AvatarHash is null ? null : $"/assets/{projection.AvatarHash}/avatar";
                challengeTitle = projection.ChallengeTitle;
                category = projection.Category;
            }
        }

        var evt = new AttackEvent(
            teamName ?? string.Empty,
            teamAvatar,
            null, // score computed on client from scoreboard
            challengeTitle ?? string.Empty,
            category,
            type,
            submission.SubmitTimeUtc);

        await attackHub.Clients.Group($"AttackGame_{submission.GameId}").ReceivedAttack(evt);
    }

    public Task<Submission[]> GetRecentSubmissionsForAttackFeed(int gameId, int limit,
        CancellationToken token = default) =>
        Context.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == gameId
                        && (s.Status == AnswerResult.Accepted || s.Status == AnswerResult.WrongAnswer))
            .Include(s => s.Team)
            .Include(s => s.GameChallenge)
            .OrderByDescending(s => s.SubmitTimeUtc)
            .Take(limit)
            .ToArrayAsync(token);


    private IQueryable<Submission> GetSubmissionsByType(AnswerResult? type = null)
    {
        var subs = type is not null
            ? Context.Submissions.Where(s => s.Status == type.Value)
            : Context.Submissions;

        return subs.OrderByDescending(s => s.SubmitTimeUtc);
    }
}
