using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Exercise;

public class ExerciseDetailModel
{
    /// <summary>
    /// Exercise ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Exercise content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Exercise category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Exercise hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Exercise credits
    /// </summary>
    public bool Credit { get; set; }

    /// <summary>
    /// Difficulty of the exercise, used for tags, sorting, etc.
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Additional tags for the exercise
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// Exercise type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag context
    /// </summary>
    public ClientFlagContext Context { get; set; } = null!;

    internal static ExerciseDetailModel FromInstance(ExerciseInstance instance) =>
        new()
        {
            Id = instance.ExerciseId,
            Content = instance.Exercise.Content,
            Hints = instance.Exercise.Hints,
            Credit = instance.Exercise.Credit,
            Difficulty = instance.Exercise.Difficulty,
            Category = instance.Exercise.Category,
            Tags = instance.Exercise.Tags,
            Title = instance.Exercise.Title,
            Type = instance.Exercise.Type,
            Context = new()
            {
                InstanceEntry = instance.Container?.Entry,
                CloseTime = instance.Container?.ExpectStopAt,
                Url = instance.AttachmentUrl,
                FileSize = instance.Attachment?.FileSize
            }
        };
}
