using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CTFServer.Test
{
    public class AccountTest : IClassFixture<TestWebAppFactory>
    {
        private readonly TestWebAppFactory _factory;
        private readonly ITestOutputHelper _output;

        public AccountTest(TestWebAppFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        [Fact]
        public async Task TestCreateUser()
        {
            using var client = _factory.CreateClient();
            var registerResult = await client.PostAsJsonAsync("/api/account/register", new
            {
                userName = "foo",
                password = "foo12345",
                email = "foo@example.com",
            });
            Assert.Equal(HttpStatusCode.BadRequest, registerResult.StatusCode);

            registerResult = await client.PostAsJsonAsync("/api/account/register", new
            {
                userName = "foo",
                password = "foo12345##Foo",
                email = "foo@example.com",
            });
            Assert.True(registerResult.IsSuccessStatusCode);

            var loginResult = await client.PostAsJsonAsync("/api/account/login", new
            {
                userName = "foo",
                password = "foo12345##"
            });
            Assert.False(loginResult.IsSuccessStatusCode);

            loginResult = await client.PostAsJsonAsync("/api/account/login", new
            {
                userName = "foo",
                password = "foo12345##Foo"
            });
            Assert.True(loginResult.IsSuccessStatusCode);
        }
    }
}
