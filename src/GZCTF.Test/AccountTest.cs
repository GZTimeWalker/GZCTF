using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace GZCTF.Test;

public class AccountTest(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task TestCreateUser()
    {
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage registerResult = await client.PostAsJsonAsync("/api/account/register",
            new { userName = "foo", password = "foo12345", email = "foo@example.com" });
        Assert.Equal(HttpStatusCode.BadRequest, registerResult.StatusCode);

        registerResult = await client.PostAsJsonAsync("/api/account/register",
            new { userName = "foo", password = "foo12345##Foo", email = "foo@example.com" });
        Assert.True(registerResult.IsSuccessStatusCode);

        HttpResponseMessage loginResult =
            await client.PostAsJsonAsync("/api/account/login", new { userName = "foo", password = "foo12345##" });
        Assert.False(loginResult.IsSuccessStatusCode);

        loginResult =
            await client.PostAsJsonAsync("/api/account/login", new { userName = "foo", password = "foo12345##Foo" });
        Assert.True(loginResult.IsSuccessStatusCode);
    }
}
