﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test;

public class AccountTest : IClassFixture<TestWebAppFactory>
{
    readonly TestWebAppFactory _factory;
    readonly ITestOutputHelper _output;

    public AccountTest(TestWebAppFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task TestCreateUser()
    {
        using HttpClient client = _factory.CreateClient();
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