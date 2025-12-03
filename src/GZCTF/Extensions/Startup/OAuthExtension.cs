using GZCTF.Services.OAuth;

namespace GZCTF.Extensions.Startup;

static class OAuthExtension
{
    public static void ConfigureOAuth(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IOAuthService, OAuthService>();
    }
}
