using System.Net;

namespace GZCTF.Services.Container.ThirdParty;

public sealed class ThirdPartyRequestException : Exception
{
    public ThirdPartyRequestException(HttpStatusCode statusCode, string body)
        : base($"Third-party request failed with status {(int)statusCode}.")
    {
        StatusCode = statusCode;
        Body = body;
    }

    public HttpStatusCode StatusCode { get; }
    public string Body { get; }
}
