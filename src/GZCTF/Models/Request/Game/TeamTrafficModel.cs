using GZCTF.Utils;

namespace GZCTF.Models;

/// <summary>
/// 队伍流量获取信息
/// </summary>
public class TeamTrafficModel
{
    /// <summary>
    /// 队伍 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 参与 Id
    /// </summary>
    public int ParticipationId { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 队伍签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 题目所捕获到的流量
    /// </summary>
    public List<FileRecord> Records { get; set; } = new();

    internal static TeamTrafficModel FromParticipation(Participation part, int challengeId)
    {
        string trafficPath = $"{FilePath.Capture}/{challengeId}/{part.Id}";

        var records = new List<FileRecord>();

        if (Directory.Exists(trafficPath))
        {
            foreach (string file in Directory.EnumerateFiles(trafficPath))
            {
                var info = new FileInfo(file)!;

                records.Add(new()
                {
                    FileName = info.Name,
                    Size = info.Length,
                    UpdateTime = info.LastAccessTimeUtc
                });
            }
        }

        return new()
        {
            Id = part.Team.Id,
            Name = part.Team.Name,
            Bio = part.Team.Bio,
            ParticipationId = part.Id,
            Avatar = part.Team.AvatarUrl,
            Records = records
        };
    }
}
