using CTFServer.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CTFServer.Test;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    static TestWebAppFactory()
    {
        Program.IsTesting = true;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(IMailSender)));
            services.AddTransient<IMailSender, TestMailSender>();
        });
    }
}