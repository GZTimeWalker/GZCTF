﻿using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

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
            Tag = challenge.Tag,
            Type = challenge.Type,
            IsEnabled = challenge.IsEnabled,
            Count = Directory.Exists(trafficPath)
                ? Directory.GetDirectories(trafficPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0
        };
    }
}
