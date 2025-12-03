namespace GZCTF.Services.OAuth;

public enum OAuthLoginError
{
    EmailInUse,
    ProviderMismatch,
    MetadataInvalid,
    ProviderMissing
}

public class OAuthLoginException(OAuthLoginError errorCode, string message) : Exception(message)
{
    public OAuthLoginError ErrorCode { get; } = errorCode;

    public string QueryCode => ErrorCode switch
    {
        OAuthLoginError.EmailInUse => "oauth_email_in_use",
        OAuthLoginError.ProviderMismatch => "oauth_provider_mismatch",
        OAuthLoginError.MetadataInvalid => "oauth_metadata_invalid",
        OAuthLoginError.ProviderMissing => "oauth_provider_missing",
        _ => "oauth_error"
    };
}
