using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class CheatInfoRepository(AppDbContext context,
    IParticipationRepository participationRepository) : RepositoryBase(context), ICheatInfoRepository
{
    public async Task<CheatInfo> CreateCheatInfo(Submission submission, Instance source, CancellationToken token = default)
    {
        var submit = await participationRepository.GetParticipation(submission.Team, submission.Game, token);

        if (submit is null)
            throw new NullReferenceException(nameof(submit));

        CheatInfo info = new()
        {
            GameId = submission.GameId,
            Submission = submission,
            SubmitTeam = submit,
            SourceTeam = source.Participation
        };

        await context.AddAsync(info, token);

        return info;
    }

    public Task<CheatInfo[]> GetCheatInfoByGameId(int gameId, CancellationToken token = default)
        => context.CheatInfo.IgnoreAutoIncludes().Where(i => i.GameId == gameId)
            .Include(i => i.SourceTeam).ThenInclude(t => t.Team)
            .Include(i => i.SubmitTeam).ThenInclude(t => t.Team)
            .Include(i => i.Submission).ThenInclude(s => s.User)
            .Include(i => i.Submission).ThenInclude(s => s.Challenge)
            .AsSplitQuery().ToArrayAsync(token);
}
