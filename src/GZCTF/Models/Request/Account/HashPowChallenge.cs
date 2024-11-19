namespace GZCTF.Models.Request.Account;

/// <summary>
/// 哈希 Pow 验证
/// </summary>
public class HashPowChallenge
{
    /// <summary>
    /// 挑战 Id
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 验证挑战
    /// </summary>
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// 难度系数
    /// </summary>
    public int Difficulty { get; set; }
}
