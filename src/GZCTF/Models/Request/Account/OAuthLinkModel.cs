namespace GZCTF.Models.Request.Account;

/// <summary>
/// OAuth provider linking request
/// </summary>
public class OAuthLinkModel
{
    /// <summary>
    /// Provider name (google, github, microsoft, etc.)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// OAuth callback model
/// </summary>
public class OAuthCallbackModel
{
    /// <summary>
    /// Authorization code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Error from OAuth provider
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Error description from OAuth provider
    /// </summary>
    public string? ErrorDescription { get; set; }
}
