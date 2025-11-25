namespace GZCTF.Services.OAuth;

public class OAuthLoginException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
