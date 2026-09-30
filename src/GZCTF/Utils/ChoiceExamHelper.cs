using System.Text.Json;
using GZCTF.Models.Request.Game;

namespace GZCTF.Utils;

/// <summary>Private answer keys are kept separate from the player response model.</summary>
public class ChoiceQuestionSnapshot : ChoicePaperQuestion
{
    public int[] CorrectAnswers { get; set; } = [];
}

public static class ChoiceExamHelper
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ChoiceExamConfigModel ReadConfig(ChoiceExam exam) =>
        JsonSerializer.Deserialize<ChoiceExamConfigModel>(exam.ConfigurationJson, JsonOptions)!;

    public static ChoiceQuestionSnapshot[] ReadQuestions(ChoiceAttempt attempt) =>
        JsonSerializer.Deserialize<ChoiceQuestionSnapshot[]>(attempt.QuestionsJson, JsonOptions)!;

    public static Dictionary<int, int[]> ReadAnswers(ChoiceAttempt attempt) =>
        JsonSerializer.Deserialize<Dictionary<int, int[]>>(attempt.AnswersJson, JsonOptions)!;

    public static ChoiceAttempt CreateAttempt(int participationId, int gameId, ChoiceExamConfigModel config)
    {
        var selected = config.Questions.Where(q => q.Type == ChoiceQuestionType.Single)
            .OrderBy(_ => Guid.NewGuid()).Take(config.SingleCount)
            .Concat(config.Questions.Where(q => q.Type == ChoiceQuestionType.Multiple)
                .OrderBy(_ => Guid.NewGuid()).Take(config.MultipleCount));
        var questions = selected.Select((q, i) => new ChoiceQuestionSnapshot
        {
            Id = i + 1, Type = q.Type, Content = q.Content, Options = q.Options,
            CorrectAnswers = q.CorrectAnswers,
            Score = q.Type == ChoiceQuestionType.Single ? config.SingleScore : config.MultipleScore
        }).ToArray();
        return new ChoiceAttempt
        {
            ParticipationId = participationId, GameId = gameId,
            QuestionsJson = JsonSerializer.Serialize(questions, JsonOptions), UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static string? SaveAnswer(ChoiceAttempt attempt, int questionId, ChoiceAnswerModel model)
    {
        if (attempt.SubmittedAt.HasValue) return "答卷已提交，不可修改。";
        if (attempt.Version != model.Version) return "答卷已在其他窗口或由队友更新，请刷新后重试。";
        var question = ReadQuestions(attempt).FirstOrDefault(q => q.Id == questionId);
        if (question is null) return "题目不存在。";
        var selected = model.SelectedOptions;
        if (selected is null || selected.Distinct().Count() != selected.Length ||
            selected.Any(a => a < 0 || a >= question.Options.Length) ||
            (question.Type == ChoiceQuestionType.Single && selected.Length > 1))
            return "选择的选项无效。";
        var answers = ReadAnswers(attempt);
        if (selected.Length == 0) answers.Remove(questionId);
        else answers[questionId] = selected.Order().ToArray();
        attempt.AnswersJson = JsonSerializer.Serialize(answers, JsonOptions);
        attempt.UpdatedAt = DateTimeOffset.UtcNow;
        attempt.Version++;
        return null;
    }

    public static string? Submit(ChoiceAttempt attempt, int version)
    {
        // Idempotent retry after a lost response never grades or awards points twice.
        if (attempt.SubmittedAt.HasValue) return null;
        if (attempt.Version != version) return "答卷已在其他窗口或由队友更新，请刷新后重试。";
        var questions = ReadQuestions(attempt);
        var answers = ReadAnswers(attempt);
        if (questions.Length == 0 || questions.Any(q => !answers.TryGetValue(q.Id, out var a) || a.Length == 0))
            return "请完成所有题目并保存后再提交。";
        attempt.Score = questions.Where(q => q.CorrectAnswers.Order().SequenceEqual(answers[q.Id].Order()))
            .Sum(q => q.Score);
        attempt.SubmittedAt = attempt.UpdatedAt = DateTimeOffset.UtcNow;
        attempt.Version++;
        return null;
    }

    public static ChoiceAttemptModel ToModel(ChoiceAttempt attempt) => new()
    {
        Questions = ReadQuestions(attempt).Select(q => new ChoicePaperQuestion
        { Id = q.Id, Type = q.Type, Content = q.Content, Options = q.Options, Score = q.Score }).ToArray(),
        Answers = ReadAnswers(attempt), Version = attempt.Version, UpdatedAt = attempt.UpdatedAt,
        SubmittedAt = attempt.SubmittedAt, Score = attempt.Score
    };
}
