namespace GZCTF.Models.Request.Account;

/// <summary>
/// Hash Pow verification
/// </summary>
public class HashPowChallenge
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Verification challenge
    /// </summary>
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    public int Difficulty { get; set; }
}
