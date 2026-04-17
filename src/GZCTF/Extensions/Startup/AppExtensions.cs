using System.Reflection;
using GZCTF.Hubs;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;

namespace GZCTF.Extensions.Startup;

internal static class AppExtensions
{
    private static readonly StaticFileOptions DefaultStaticFileOptions = new()
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.GetTypedHeaders().CacheControl = new()
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(7)
            };
        }
    };

    private static readonly WebSocketOptions DefaultWebSocketOptions =
        new() { KeepAliveInterval = TimeSpan.FromMinutes(30) };

    extension(WebApplication app)
    {
        internal async Task RunServerAsync()
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

        internal void UseMiddlewares()
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
            app.Use(async (context, next) =>
            {
                var fingerprint = ContextHelper.GetValidBrowserFingerprint(context.User);

                if (string.IsNullOrWhiteSpace(fingerprint))
                {
                    await next();
                    return;
                }

                using (LogContext.PushProperty("BrowserFingerprint", fingerprint))
                {
                    await next();
                }
            });
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
            app.MapHub<AttackHub>("/hub/attack");

            app.UseIndexAsync();
        }
    }
}
