namespace GZCTF.Models.Request.Game;

/// <summary>
/// 队伍流量获取信息
/// </summary>
public class TeamTrafficModel
{
    /// <summary>
    /// 参与 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍 Id
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 参赛所属组织
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 题目所捕获到的流量数量
    /// </summary>
    public int Count { get; set; }

    internal static TeamTrafficModel FromParticipation(Participation part, int challengeId)
    {
        var trafficPath = $"{FilePath.Capture}/{challengeId}/{part.Id}";

        return new()
        {
            Id = part.Id,
            TeamId = part.Team.Id,
            Name = part.Team.Name,
            Organization = part.Organization,
            Avatar = part.Team.AvatarUrl,
            Count = Directory.Exists(trafficPath)
                ? Directory.GetFiles(trafficPath, "*", SearchOption.TopDirectoryOnly).Length
                : 0
        };
    }
}
