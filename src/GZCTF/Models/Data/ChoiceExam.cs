using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Data;

/// <summary>One choice question bank and configuration per game.</summary>
public class ChoiceExam
{
    [Key]
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public bool Enabled { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    [ConcurrencyCheck]
    public int Version { get; set; }
}

/// <summary>A team's fixed paper, durable draft and immutable final result.</summary>
public class ChoiceAttempt
{
    [Key]
    public int ParticipationId { get; set; }
    public Participation Participation { get; set; } = null!;
    public int GameId { get; set; }
    public ChoiceExam Exam { get; set; } = null!;
    public string QuestionsJson { get; set; } = "[]";
    public string AnswersJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int? Score { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; } = 1;
}
