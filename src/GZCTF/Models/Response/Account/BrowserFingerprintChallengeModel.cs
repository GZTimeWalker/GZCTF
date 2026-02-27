namespace GZCTF.Models.Response.Account;

/// <summary>
/// Browser fingerprint challenge payload
/// </summary>
public class BrowserFingerprintChallengeModel
{
    /// <summary>
    /// Challenge nonce, valid for a short period and one-time use
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Required probe keys for this challenge
    /// </summary>
    public string[] RequiredSignals { get; set; } = [];

    /// <summary>
    /// Challenge expiration in seconds
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}
