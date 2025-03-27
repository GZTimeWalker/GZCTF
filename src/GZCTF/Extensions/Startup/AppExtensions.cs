using System.Reflection;
using GZCTF.Hubs;
using Scalar.AspNetCore;
using Serilog;

namespace GZCTF.Extensions.Startup;

static class AppExtensions
{
    static readonly StaticFileOptions DefaultStaticFileOptions = new()
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.CacheControl = $"public, max-age={60 * 60 * 24 * 7}";
        }
    };

    static readonly WebSocketOptions DefaultWebSocketOptions = new() { KeepAliveInterval = TimeSpan.FromMinutes(30) };

    internal static async Task RunServerAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Server>>();

        try
        {
            var version = typeof(Server).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            logger.SystemLog(version ?? "GZ::CTF", TaskStatus.Pending, LogLevel.Debug);
            await app.RunAsync();
        }
        catch (Exception exception)
        {
            logger.LogErrorMessage(exception, StaticLocalizer[nameof(Resources.Program.Server_Failed)]);
            throw;
        }
        finally
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Server_Exited)], TaskStatus.Exit,
                LogLevel.Debug);

            await Log.CloseAndFlushAsync();
        }
    }

    internal static void UseMiddlewares(this WebApplication app)
    {
        app.UseRequestLocalization();

        app.UseResponseCaching();
        app.UseResponseCompression();

        app.UseCustomFavicon();
        app.UseStaticFiles(DefaultStaticFileOptions);

        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseOpenApi(options =>
            {
                options.PostProcess += (document, _) => document.Servers.Clear();
                options.Path = "/openapi/{documentName}.json";
            });
            // open ui in `/scalar/v1`
            app.MapScalarApiReference();
        }
        else
        {
            app.UseExceptionHandler("/error/500");
            app.UseHsts();
        }

        app.UseRouting();

        if (app.Configuration.GetValue<bool>("DisableRateLimit") is not true)
            app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("RequestLogging"))
            app.UseRequestLogging();

        app.UseWebSockets(DefaultWebSocketOptions);
        app.UseTelemetry();

        app.MapHealthCheck();
        app.MapControllers();

        app.MapHub<UserHub>("/hub/user");
        app.MapHub<MonitorHub>("/hub/monitor");
        app.MapHub<AdminHub>("/hub/admin");

        app.UseIndexAsync();
    }
}
