using FluentStorage;
using FluentStorage.Blobs;

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

    internal static async Task<TeamTrafficModel> FromParticipation(Participation part, int challengeId,
        IBlobStorage storage, CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, challengeId.ToString(), part.Id.ToString());

        return new()
        {
            Id = part.Id,
            TeamId = part.Team.Id,
            Name = part.Team.Name,
            Organization = part.Organization,
            Avatar = part.Team.AvatarUrl,
            Count = await storage.ExistsAsync(path, token)
                ? (await storage.ListAsync(path, cancellationToken: token)).Count
                : 0
        };
    }
}
