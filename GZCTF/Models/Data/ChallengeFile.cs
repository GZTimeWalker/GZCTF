using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class ChallengeFile : FileBase
{
    /// <summary>
    /// 题目数据库Id
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// 题目
    /// </summary>
    [JsonIgnore]
    public Challenge? Challenge { get; set; }
}

