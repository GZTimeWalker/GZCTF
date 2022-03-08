using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class PuzzleRepository : RepositoryBase, IPuzzleRepository
{
    private static readonly Logger logger = LogManager.GetLogger("PuzzleRepository");

    public PuzzleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Puzzle> AddPuzzle(PuzzleBase newPuzzle, CancellationToken token)
    {
        Puzzle puzzle = new(newPuzzle);

        await context.AddAsync(puzzle, token);
        await context.SaveChangesAsync(token);

        LogHelper.SystemLog(logger, $"添加题目#{puzzle.Id} {puzzle.Title} 成功");

        return puzzle;
    }

    public async Task<(bool result, string title)> DeletePuzzle(int id, CancellationToken token)
    {
        Puzzle? puzzle = await context.Puzzles.FirstOrDefaultAsync(x => x.Id == id, token);

        if (puzzle is null)
        {
            LogHelper.SystemLog(logger, $"未找到试图删除的题目", TaskStatus.Fail);
            return (false, string.Empty);
        }

        string title = puzzle.Title!;

        context.Remove(puzzle);
        await context.SaveChangesAsync(token);

        return (true, title);
    }

    public Task<List<PuzzleItem>> GetAccessiblePuzzles(int accessLevel, CancellationToken token)
        => (from p in context.Puzzles.Where(p => p.AccessLevel <= accessLevel).OrderBy(p => p.AccessLevel)
                select new PuzzleItem()
                {
                    Id = p.Id,
                    Title = p.Title,
                    AcceptedCount = p.AcceptedCount,
                    SubmissionCount = p.SubmissionCount,
                    Score = p.CurrentScore
                }).ToListAsync(token);

    public int GetMaxAccessLevel()
        => context.Puzzles.Any() switch
        {
            true => context.Puzzles.Max(p => p.UpgradeAccessLevel),
            false => 0
        };

    public async Task<UserPuzzleModel?> GetUserPuzzle(int id, int accessLevel, CancellationToken token)
    {
        Puzzle? puzzle = await context.Puzzles.FirstOrDefaultAsync(x => x.Id == id, token);

        if (puzzle is null || puzzle.AccessLevel > accessLevel)
        {
            LogHelper.SystemLog(logger, $"未找到满足要求的题目", TaskStatus.Fail);
            return null;
        }

        return new UserPuzzleModel
        {
            Title = puzzle.Title,
            Content = puzzle.Content,
            ClientJS = puzzle.ClientJS,
            AcceptedCount = puzzle.AcceptedUserCount,
            SubmissionCount = puzzle.SubmissionCount
        };
    }

    public async Task<Puzzle?> UpdatePuzzle(int id, PuzzleBase newPuzzle, CancellationToken token)
    {
        Puzzle? puzzle = await context.Puzzles.FirstOrDefaultAsync(x => x.Id == id, token);

        if (puzzle is null)
        {
            LogHelper.SystemLog(logger, $"未找到满足要求的题目", TaskStatus.Fail);
            return null;
        }

        puzzle.Update(newPuzzle);
        await context.SaveChangesAsync(token);

        LogHelper.SystemLog(logger, $"成功更新题目#{puzzle.Id}");

        return puzzle;
    }

    public async Task<VerifyResult> VerifyAnswer(int id, string? answer, UserInfo user, bool hasSolved, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            LogHelper.SystemLog(logger, "答案验证遇到空输入", TaskStatus.Fail);
            return new VerifyResult();
        }

        Puzzle? puzzle = await context.Puzzles.FirstOrDefaultAsync(x => x.Id == id, token);

        if (puzzle is null)
        {
            LogHelper.SystemLog(logger, $"题目未找到#{id}", TaskStatus.Fail);
            return new VerifyResult(AnswerResult.Unauthorized);
        }

        if(puzzle.AccessLevel > user.AccessLevel)
        {
            LogHelper.SystemLog(logger, $"未授权的题目访问#{id}", TaskStatus.Denied);
            return new VerifyResult(AnswerResult.Unauthorized);
        }

        bool check = string.Equals(puzzle.Answer, answer);

        var result = check ? new VerifyResult(AnswerResult.Accepted, puzzle.CurrentScore, puzzle.UpgradeAccessLevel)
                : new VerifyResult(AnswerResult.WrongAnswer);

        if (user.Privilege != Privilege.User)
            return result;

        ++puzzle.SubmissionCount;

        if (check)
        {
            ++puzzle.AcceptedCount;

            if (!hasSolved)
                ++puzzle.AcceptedUserCount;
        }

        await context.SaveChangesAsync(token);

        return result;
    }
}
