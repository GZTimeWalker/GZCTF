using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Exercise;

public class ExerciseDetailModel
{
    /// <summary>
    /// 题目 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类别
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// 练习题目的积分
    /// </summary>
    public bool Credit { get; set; }

    /// <summary>
    /// 练习题目的难度，用作标签、排序等
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// 练习题目附加标签
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag 上下文
    /// </summary>
    public ClientFlagContext Context { get; set; } = default!;

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
