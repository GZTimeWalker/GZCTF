using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Request.Edit;

namespace GZCTF.Models.Data;

public class GameChallenge : Challenge
{
    /// <summary>
    /// 是否需要记录访问流量
    /// </summary>
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// 初始分数
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 500;

    /// <summary>
    /// 最低分数比例
    /// </summary>
    [Required]
    [Range(0, 1)]
    public double MinScoreRate { get; set; } = 0.25;

    /// <summary>
    /// 难度系数
    /// </summary>
    [Required]
    public double Difficulty { get; set; } = 5;

    /// <summary>
    /// 当前题目分值
    /// </summary>
    [NotMapped]
    public int CurrentScore =>
        AcceptedCount <= 1
            ? OriginalScore
            : (int)Math.Floor(
                OriginalScore * (MinScoreRate +
                                 (1.0 - MinScoreRate) * Math.Exp((1 - AcceptedCount) / Difficulty)
                ));

    internal string GenerateDynamicFlag(Participation part)
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);
        
        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        //   Using the signature private key of the game to generate a hash for the
        // team is not a wise and sufficiently secure choice. Moreover, this private
        // key should not exist outside of any backend systems, even if it is encrypted
        // with a XOR key in a configuration file or provided to the organizers (admin)
        // for third-party flag calculation and external distribution.
        //   To address this issue, one possible solution is to use a salted hash of
        // the private key as the salt for the team's hash.
        var hash = $"{part.Token}::{part.Game.TeamHashSalt}::{Id}".ToSHA256String();
        return flag.Replace("[TEAM_HASH]", hash[12..24]);
    }

    internal void Update(ChallengeUpdateModel model)
    {
        Title = model.Title ?? Title;
        Content = model.Content ?? Content;
        Tag = model.Tag ?? Tag;
        Hints = model.Hints ?? Hints;
        IsEnabled = model.IsEnabled ?? IsEnabled;
        CPUCount = model.CPUCount ?? CPUCount;
        MemoryLimit = model.MemoryLimit ?? MemoryLimit;
        StorageLimit = model.StorageLimit ?? StorageLimit;
        ContainerImage = model.ContainerImage?.Trim() ?? ContainerImage;
        ContainerExposePort = model.ContainerExposePort ?? ContainerExposePort;
        OriginalScore = model.OriginalScore ?? OriginalScore;
        MinScoreRate = model.MinScoreRate ?? MinScoreRate;
        Difficulty = model.Difficulty ?? Difficulty;
        FileName = model.FileName ?? FileName;

        // only set FlagTemplate to null when it pass an empty string (but not null)
        FlagTemplate = model.FlagTemplate is null ? FlagTemplate :
            string.IsNullOrWhiteSpace(model.FlagTemplate) ? null : model.FlagTemplate;

        // DynamicContainer only
        EnableTrafficCapture = Type == ChallengeType.DynamicContainer &&
                               (model.EnableTrafficCapture ?? EnableTrafficCapture);
    }

    #region Db Relationship
    
    /// <summary>
    /// 题目对应的 Flag 列表
    /// </summary>
    public List<FlagContext> Flags { get; set; } = new();

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    /// <summary>
    /// 赛题实例
    /// </summary>
    public List<GameInstance> Instances { get; set; } = new();

    /// <summary>
    /// 激活赛题的队伍
    /// </summary>
    public HashSet<Participation> Teams { get; set; } = new();

    /// <summary>
    /// 比赛 Id
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

    #endregion Db Relationship
}