using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class CheatInfoRepository(
    AppDbContext context,
    IParticipationRepository participationRepository) : RepositoryBase(context), ICheatInfoRepository
{
    public async Task<CheatInfo> CreateCheatInfo(Submission submission, GameInstance source,
        CancellationToken token = default)
    {
        Participation? submit = await participationRepository.GetParticipation(submission.Team, submission.Game, token);

        if (submit is null)
            throw new NullReferenceException(nameof(submit));

        CheatInfo info = new()
        {
            GameId = submission.GameId,
            Submission = submission,
            SubmitTeam = submit,
            SourceTeam = source.Participation
        };

        await Context.AddAsync(info, token);

        return info;
    }

    public Task<CheatInfo[]> GetCheatInfoByGameId(int gameId, CancellationToken token = default) =>
        Context.CheatInfo.IgnoreAutoIncludes().Where(i => i.GameId == gameId)
            .Include(i => i.SourceTeam).ThenInclude(t => t.Team)
            .Include(i => i.SubmitTeam).ThenInclude(t => t.Team)
            .Include(i => i.Submission).ThenInclude(s => s.User)
            .Include(i => i.Submission).ThenInclude(s => s.GameChallenge)
            .AsSplitQuery().ToArrayAsync(token);
}
