using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

public class ChallengeTrafficModel
{
    /// <summary>
    /// 题目Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目类别
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 题目所捕获到的队伍流量数量
    /// </summary>
    public int Count { get; set; }

    internal static ChallengeTrafficModel FromChallenge(GameChallenge challenge)
    {
        var trafficPath = $"{FilePath.Capture}/{challenge.Id}";

        return new()
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Category = challenge.Category,
            Type = challenge.Type,
            IsEnabled = challenge.IsEnabled,
            Count = Directory.Exists(trafficPath)
                ? Directory.GetDirectories(trafficPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0
        };
    }
}
