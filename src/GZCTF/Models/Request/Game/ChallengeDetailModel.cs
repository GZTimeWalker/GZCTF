using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Challenge detailed information
/// </summary>
public class ChallengeDetailModel
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Current score of the challenge
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Challenge type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag context
    /// </summary>
    public ClientFlagContext Context { get; set; } = null!;

    /// <summary>
    /// Maximum number of attempts allowed (0 = no limit)
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Current attempt count
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Deadline of the challenge, null means no deadline
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }

    internal static ChallengeDetailModel FromInstance(GameInstance gameInstance, int attemptCount,
        ChallengeInfo? scoreboardChallenge = null) =>
        new()
        {
            Id = gameInstance.Challenge.Id,
            Content = gameInstance.Challenge.Content,
            Hints = gameInstance.Challenge.Hints,
            Score = scoreboardChallenge?.Score ?? gameInstance.Challenge.CurrentScore,
            Category = gameInstance.Challenge.Category,
            Title = gameInstance.Challenge.Title,
            Type = gameInstance.Challenge.Type,
            Limit = gameInstance.Challenge.SubmissionLimit,
            Deadline = gameInstance.Challenge.DeadlineUtc,
            Attempts = attemptCount,
            Context = new()
            {
                InstanceEntry = gameInstance.Container?.Entry,
                CloseTime = gameInstance.Container?.ExpectStopAt,
                Url = gameInstance.AttachmentUrl,
                FileSize = gameInstance.Attachment?.FileSize
            }
        };
}
