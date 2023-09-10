﻿namespace GZCTF.Models.Request.Game;

/// <summary>
/// 题目详细信息
/// </summary>
public class ChallengeDetailModel
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
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// 题目当前分值
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag 上下文
    /// </summary>
    public ClientFlagContext Context { get; set; } = default!;

    internal static ChallengeDetailModel FromInstance(Instance instance) =>
        new()
        {
            Id = instance.Challenge.Id,
            Content = instance.Challenge.Content,
            Hints = instance.Challenge.Hints,
            Score = instance.Challenge.CurrentScore,
            Tag = instance.Challenge.Tag,
            Title = instance.Challenge.Title,
            Type = instance.Challenge.Type,
            Context = new()
            {
                InstanceEntry = instance.Container?.Entry,
                CloseTime = instance.Container?.ExpectStopAt,
                Url = instance.AttachmentUrl,
                FileSize = instance.Attachment?.FileSize
            }
        };
}

public class ClientFlagContext
{
    /// <summary>
    /// 题目实例的关闭时间
    /// </summary>
    public DateTimeOffset? CloseTime { get; set; }

    /// <summary>
    /// 题目实例的连接方式
    /// </summary>
    public string? InstanceEntry { get; set; }

    /// <summary>
    /// 附件 Url
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 附件文件大小
    /// </summary>
    public long? FileSize { get; set; }
}