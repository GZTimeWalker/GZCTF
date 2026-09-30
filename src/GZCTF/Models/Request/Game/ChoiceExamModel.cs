using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

[JsonConverter(typeof(JsonStringEnumConverter<ChoiceQuestionType>))]
public enum ChoiceQuestionType { Single, Multiple }

public class ChoiceQuestionModel
{
    public ChoiceQuestionType Type { get; set; }
    [Required, StringLength(10000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
    [Required, MinLength(2), MaxLength(26)]
    public string[] Options { get; set; } = [];
    /// <summary>Zero-based option indices. Only returned to administrators.</summary>
    [Required, MinLength(1), MaxLength(26)]
    public int[] CorrectAnswers { get; set; } = [];
}

public class ChoiceExamConfigModel : IValidatableObject
{
    public bool Enabled { get; set; }
    [Range(0, 1000)]
    public int SingleCount { get; set; }
    [Range(0, 1000)]
    public int MultipleCount { get; set; }
    [Range(1, 10000)]
    public int SingleScore { get; set; } = 1;
    [Range(1, 10000)]
    public int MultipleScore { get; set; } = 2;
    [Range(0, int.MaxValue)]
    public int Version { get; set; }
    public bool Locked { get; set; }
    [Required, MaxLength(5000)]
    public ChoiceQuestionModel[] Questions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Questions is null) yield break;
        if (Enabled && SingleCount + MultipleCount == 0)
            yield return new ValidationResult("启用选择题时至少配置一道题目。");
        if (SingleCount > Questions.Count(q => q?.Type == ChoiceQuestionType.Single) ||
            MultipleCount > Questions.Count(q => q?.Type == ChoiceQuestionType.Multiple))
            yield return new ValidationResult("题库中的单选或多选题数量不足。");
        for (var i = 0; i < Questions.Length; i++)
        {
            var q = Questions[i];
            if (q is null || !Enum.IsDefined(q.Type) || string.IsNullOrWhiteSpace(q.Content) ||
                q.Content.Length > 10000 || q.Options is null || q.Options.Length is < 2 or > 26 ||
                q.Options.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > 2000) ||
                q.Options.Distinct().Count() != q.Options.Length || q.CorrectAnswers is null ||
                q.CorrectAnswers.Length == 0 || q.CorrectAnswers.Distinct().Count() != q.CorrectAnswers.Length ||
                q.CorrectAnswers.Any(a => a < 0 || a >= q.Options.Length) ||
                (q.Type == ChoiceQuestionType.Single && q.CorrectAnswers.Length != 1) ||
                (q.Type == ChoiceQuestionType.Multiple && q.CorrectAnswers.Length < 2))
                yield return new ValidationResult($"第 {i + 1} 道题目的题型、题干、选项或正确答案无效。");
        }
    }
}

public class ChoicePaperQuestion
{
    public int Id { get; set; }
    public ChoiceQuestionType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string[] Options { get; set; } = [];
    public int Score { get; set; }
}

public class ChoiceAttemptModel
{
    public ChoicePaperQuestion[] Questions { get; set; } = [];
    public Dictionary<int, int[]> Answers { get; set; } = [];
    public int Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int? Score { get; set; }
}

public class ChoiceAnswerModel
{
    [Range(1, int.MaxValue)]
    public int Version { get; set; }
    [Required, MaxLength(26)]
    public int[] SelectedOptions { get; set; } = [];
}

public class ChoiceSubmitModel
{
    [Range(1, int.MaxValue)]
    public int Version { get; set; }
}

public record ChoiceExamInfoModel(bool Enabled, int SingleCount, int MultipleCount, int SingleScore, int MultipleScore);
public record ChoiceResultModel(int TeamId, string TeamName, DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt, int AnsweredCount, int QuestionCount, int? Score);
