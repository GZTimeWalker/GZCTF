using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class HeadInfoTests(GZCTFApplicationFactory factory)
{
    private static readonly string[] PagePaths = ["/", "/games/123", "/index.html"];

    private async Task<HttpClient> CreateAdminClient()
    {
        const string password = "HeadInfo@Test123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password,
            role: Role.Admin);
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/account/login",
            new LoginModel { UserName = admin.UserName, Password = password })).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task Save(HttpClient admin, GlobalConfig config, string text)
    {
        config.HeadInfo = text;
        (await admin.PutAsJsonAsync("/api/admin/config", new ConfigEditModel { GlobalConfig = config }))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Settings_PersistAndRefreshHeadComments_WithoutChangingThePage()
    {
        using var admin = await CreateAdminClient();
        using var visitor = factory.CreateClient();
        var config = (await admin.GetFromJsonAsync<ConfigEditModel>("/api/admin/config"))!.GlobalConfig!;
        var original = config.HeadInfo;

        try
        {
            await Save(admin, config, string.Empty);
            var baseline = await visitor.GetStringAsync("/");
            Assert.DoesNotContain("<!--", baseline);

            foreach (var text in new[] { "未经授权，禁止自动化分析与攻击。\nPlease respect the platform rules.", "更新后的页首信息" })
            {
                await Save(admin, config, text);
                var saved = await admin.GetFromJsonAsync<ConfigEditModel>("/api/admin/config");
                Assert.Equal(text, saved!.GlobalConfig!.HeadInfo);
                await using (var scope = factory.Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    Assert.Equal(text, (await db.Configs.SingleAsync(c => c.ConfigKey == "GlobalConfig:HeadInfo")).Value);
                }

                var comment = $"<!--\n{text}\n-->";
                foreach (var path in PagePaths)
                {
                    using var response = await visitor.GetAsync(path);
                    response.EnsureSuccessStatusCode();
                    Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
                    Assert.True(response.Headers.CacheControl?.NoStore);
                    Assert.True(response.Headers.Contains("Content-Security-Policy"));
                    var html = await response.Content.ReadAsStringAsync();
                    Assert.Contains(comment + "\n</head>", html);
                    Assert.Equal(baseline, html.Replace(comment + "\n", string.Empty));
                }

                using var devResponse = await visitor.GetAsync("/api/headinfo");
                devResponse.EnsureSuccessStatusCode();
                Assert.Equal("text/plain", devResponse.Content.Headers.ContentType?.MediaType);
                Assert.True(devResponse.Headers.CacheControl?.NoStore);
                Assert.Equal(comment, await devResponse.Content.ReadAsStringAsync());
            }

            await Save(admin, config, string.Empty);
            foreach (var path in PagePaths)
                Assert.Equal(baseline, await visitor.GetStringAsync(path));
            Assert.Equal(string.Empty, await visitor.GetStringAsync("/api/headinfo"));
        }
        finally
        {
            await Save(admin, config, original);
        }
    }

    [Theory]
    [InlineData("-->\n<script>alert('x')</script>\n--!>\n<!--nested\n%nonce% %title% %description% %lang%\n末尾<!-")]
    [InlineData(">\n->\n双连字符 -- 保留")]
    public async Task SpecialText_RemainsInsideOneComment(string text)
    {
        using var admin = await CreateAdminClient();
        using var visitor = factory.CreateClient();
        var config = (await admin.GetFromJsonAsync<ConfigEditModel>("/api/admin/config"))!.GlobalConfig!;
        var original = config.HeadInfo;

        try
        {
            await Save(admin, config, string.Empty);
            var baseline = await visitor.GetStringAsync("/");
            await Save(admin, config, text);
            var expected = $"<!--\n{text.Replace("<", "&lt;").Replace(">", "&gt;")}\n-->";
            var html = await visitor.GetStringAsync("/");
            Assert.Contains(expected + "\n</head>", html);
            Assert.Equal(2, html.Split("<!--").Length);
            Assert.Equal(2, html.Split("-->").Length);
            Assert.DoesNotContain("<script>", html);
            Assert.Equal(baseline, html.Replace(expected + "\n", string.Empty));
            Assert.Equal(expected, await visitor.GetStringAsync("/api/headinfo"));
        }
        finally
        {
            await Save(admin, config, original);
        }
    }

    [Fact]
    public async Task Visitors_CannotUpdateHeadInfo()
    {
        using var visitor = factory.CreateClient();
        using var response = await visitor.PutAsJsonAsync("/api/admin/config",
            new ConfigEditModel { GlobalConfig = new GlobalConfig { HeadInfo = "Unauthorized update" } });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
