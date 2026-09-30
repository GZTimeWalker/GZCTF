using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/game/{id:int}/choice")]
[RequireUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ChoiceController(AppDbContext db, UserManager<UserInfo> userManager,
    IParticipationRepository participations, CacheHelper cache) : ControllerBase
{
    [HttpGet("info")]
    public async Task<ActionResult<ChoiceExamInfoModel>> Info(int id, CancellationToken token)
    {
        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, token);
        if (game is null || game.Hidden) return NotFound();
        var exam = await db.ChoiceExams.AsNoTracking().FirstOrDefaultAsync(e => e.GameId == id, token);
        var config = exam is null ? new ChoiceExamConfigModel() : ChoiceExamHelper.ReadConfig(exam);
        return new ChoiceExamInfoModel(config.Enabled, config.SingleCount, config.MultipleCount,
            config.SingleScore, config.MultipleScore);
    }

    [RequireAdmin]
    [HttpGet("config")]
    public async Task<ActionResult<ChoiceExamConfigModel>> Config(int id, CancellationToken token)
    {
        if (!await db.Games.AnyAsync(g => g.Id == id, token)) return NotFound();
        var exam = await db.ChoiceExams.AsNoTracking().FirstOrDefaultAsync(e => e.GameId == id, token);
        var config = exam is null ? new ChoiceExamConfigModel() : ChoiceExamHelper.ReadConfig(exam);
        config.Version = exam?.Version ?? 0;
        config.Locked = await db.ChoiceAttempts.AnyAsync(a => a.GameId == id, token);
        return config;
    }

    /// <summary>Atomically validate and replace configuration and question bank.</summary>
    [RequireAdmin]
    [HttpPut("config")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SaveConfig(int id, ChoiceExamConfigModel model, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        // The same lock is acquired when creating a paper, so config edits cannot race its snapshot.
        await LockGame(id, token);
        if (!await db.Games.AnyAsync(g => g.Id == id, token)) return NotFound();
        if (await db.ChoiceAttempts.AnyAsync(a => a.GameId == id, token))
            return Conflict(new RequestResponse("已有队伍开始答题，题库与分值不可修改。", 409));
        var exam = await db.ChoiceExams.FirstOrDefaultAsync(e => e.GameId == id, token);
        if ((exam?.Version ?? 0) != model.Version) return ConflictResponse();
        if (exam is null) db.ChoiceExams.Add(exam = new ChoiceExam { GameId = id });
        model.Locked = false;
        model.Version = ++exam.Version;
        exam.Enabled = model.Enabled;
        exam.ConfigurationJson = JsonSerializer.Serialize(model, ChoiceExamHelper.JsonOptions);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Ok(model);
    }

    [RequireAdmin]
    [HttpGet("results")]
    public async Task<ActionResult<ChoiceResultModel[]>> Results(int id, CancellationToken token)
    {
        var attempts = await db.ChoiceAttempts.AsNoTracking().Where(a => a.GameId == id)
            .Select(a => new { Attempt = a, a.Participation.TeamId, a.Participation.Team.Name })
            .ToArrayAsync(token);
        return attempts.Select(a => new ChoiceResultModel(a.TeamId, a.Name, a.Attempt.UpdatedAt,
            a.Attempt.SubmittedAt, ChoiceExamHelper.ReadAnswers(a.Attempt).Count,
            ChoiceExamHelper.ReadQuestions(a.Attempt).Length, a.Attempt.Score)).ToArray();
    }

    /// <summary>Restore the team's saved state; never creates or changes a paper.</summary>
    [HttpGet("attempt")]
    public async Task<IActionResult> Attempt(int id, CancellationToken token)
    {
        var (part, error) = await GetParticipation(id, false, token);
        if (error is not null) return error;
        var attempt = await db.ChoiceAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ParticipationId == part!.Id, token);
        return attempt is null ? NoContent() : Ok(ChoiceExamHelper.ToModel(attempt));
    }

    [HttpPost("attempt")]
    public async Task<IActionResult> Start(int id, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockGame(id, token);
        var (part, error) = await GetParticipation(id, true, token);
        if (error is not null) return error;
        var attempt = await db.ChoiceAttempts.FirstOrDefaultAsync(a => a.ParticipationId == part!.Id, token);
        if (attempt is null)
        {
            var exam = await db.ChoiceExams.SingleAsync(e => e.GameId == id, token);
            attempt = ChoiceExamHelper.CreateAttempt(part!.Id, id, ChoiceExamHelper.ReadConfig(exam));
            db.ChoiceAttempts.Add(attempt);
            await db.SaveChangesAsync(token);
        }
        await transaction.CommitAsync(token);
        return Ok(ChoiceExamHelper.ToModel(attempt));
    }

    [HttpPut("answers/{questionId:int}")]
    public async Task<IActionResult> SaveAnswer(int id, int questionId, ChoiceAnswerModel model,
        CancellationToken token)
    {
        var (part, error) = await GetParticipation(id, true, token);
        if (error is not null) return error;
        var attempt = await db.ChoiceAttempts.FirstOrDefaultAsync(a => a.ParticipationId == part!.Id, token);
        if (attempt is null) return NotFound();
        if (attempt.SubmittedAt.HasValue || attempt.Version != model.Version) return ConflictResponse();
        var validationError = ChoiceExamHelper.SaveAnswer(attempt, questionId, model);
        if (validationError is not null) return BadRequest(new RequestResponse(validationError));
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return ConflictResponse(); }
        return Ok(ChoiceExamHelper.ToModel(attempt));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit(int id, ChoiceSubmitModel model, CancellationToken token)
    {
        var (part, error) = await GetParticipation(id, false, token);
        if (error is not null) return error;
        var attempt = await db.ChoiceAttempts.FirstOrDefaultAsync(a => a.ParticipationId == part!.Id, token);
        if (attempt is null) return NotFound();
        if (!attempt.SubmittedAt.HasValue)
        {
            if (!part!.Game.PracticeMode && DateTimeOffset.UtcNow >= part.Game.EndTimeUtc)
                return BadRequest(new RequestResponse("比赛已结束。"));
            if (attempt.Version != model.Version) return ConflictResponse();
            var validationError = ChoiceExamHelper.Submit(attempt, model.Version);
            if (validationError is not null) return BadRequest(new RequestResponse(validationError));
            try { await db.SaveChangesAsync(token); }
            catch (DbUpdateConcurrencyException) { return ConflictResponse(); }
        }
        await cache.FlushScoreboardCache(id, token);
        return Ok(ChoiceExamHelper.ToModel(attempt));
    }

    private Task<int> LockGame(int id, CancellationToken token) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Games\" WHERE \"Id\" = {id} FOR UPDATE", token);

    private IActionResult ConflictResponse() => Conflict(new RequestResponse("答卷已锁定或数据已更新，请刷新后重试。", 409));

    private async Task<(Participation? Part, IActionResult? Error)> GetParticipation(int id, bool write,
        CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return (null, Unauthorized());
        var part = await participations.GetParticipation(user.Id, id, token);
        if (part is null || part.Status != ParticipationStatus.Accepted) return (null, Forbid());
        if (DateTimeOffset.UtcNow < part.Game.StartTimeUtc)
            return (null, BadRequest(new RequestResponse("比赛尚未开始。")));
        if (write && !part.Game.PracticeMode && DateTimeOffset.UtcNow >= part.Game.EndTimeUtc)
            return (null, BadRequest(new RequestResponse("比赛已结束。")));
        if (!await db.ChoiceExams.AnyAsync(e => e.GameId == id && e.Enabled, token))
            return (null, NotFound(new RequestResponse("本场比赛未启用选择题。", 404)));
        return (part, null);
    }
}
