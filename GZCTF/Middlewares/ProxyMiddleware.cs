using System.Net;

namespace CTFServer.Middlewares;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;

    public ProxyMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (headers.ContainsKey("X-Forwarded-For"))
        {
            var ipAddresses = headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddresses.FirstOrDefault() ?? "0.0.0.0");
        }
        return _next(context);
    }
}
