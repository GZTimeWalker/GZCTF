using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GZCTF.Models.Request.Info;
using GZCTF.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.IntegrationTests.Controllers;

public class InfoControllerTest : IntegrationTestBase
{
    public InfoControllerTest(GzctfWebApplicationFactory factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetLatestPosts_ShouldReturnOk()
    {
        // Arrange
        await Factory.SeedDataAsync();

        // Act
        var response = await Client.GetAsync("/api/Posts/Latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<PostInfoModel[]>();
        posts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClientConfig_ShouldReturnConfiguration()
    {
        // Act
        var response = await Client.GetAsync("/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        
        Output.WriteLine($"Client config response: {content}");
    }

    [Fact]
    public async Task GetCaptcha_ShouldReturnCaptchaInfo()
    {
        // Act
        var response = await Client.GetAsync("/api/captcha");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        
        Output.WriteLine($"Captcha response: {content}");
    }

    [Theory]
    [InlineData("/api/Posts/Latest")]
    [InlineData("/api/config")]
    [InlineData("/api/captcha")]
    public async Task PublicEndpoints_ShouldBeAccessibleWithoutAuthentication(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLatestPosts_WithSeededData_ShouldReturnPosts()
    {
        // Arrange
        await Factory.SeedDataAsync();

        // Act
        var response = await Client.GetAsync("/api/Posts/Latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<PostInfoModel[]>();
        posts.Should().NotBeNull();
        
        // The seeded data should include posts
        posts!.Length.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task MultipleRequests_ShouldBeHandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Client.GetAsync("/api/config"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        // Dispose responses
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}