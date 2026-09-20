using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Learning;

[Collection(nameof(IntegrationTestCollection))]
public class EnrollmentTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task LearnerCanJoinMultiplePaths_SelectOneCurrent_AndLeaveIt()
    {
        var first = await SeedPublishedPathAsync("first");
        var second = await SeedPublishedPathAsync("second");
        var user = await CreateUserAsync();
        using var client = await LoginAsync(user);

        var firstJoin = await client.PostAsync($"/api/learning-paths/{first.PathId}/enroll", null);
        firstJoin.EnsureSuccessStatusCode();
        var secondJoin = await client.PostAsync($"/api/learning-paths/{second.PathId}/enroll", null);
        secondJoin.EnsureSuccessStatusCode();

        var select = await client.PostAsync($"/api/learning-paths/{second.PathId}/select", null);
        select.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var enrollments = await db.Enrollments.Where(item => item.UserId == user.Id).ToListAsync();
            Assert.Equal(2, enrollments.Count);
            Assert.Equal(second.PathId, Assert.Single(enrollments, item => item.IsCurrent).PathId);
        }

        var list = await client.GetAsync("/api/learning-paths/enrollments");
        list.EnsureSuccessStatusCode();
        var listJson = await list.Content.ReadAsStringAsync();
        Assert.Contains(first.Slug, listJson);
        Assert.Contains(second.Slug, listJson);

        var leave = await client.DeleteAsync($"/api/learning-paths/{second.PathId}/enroll");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var enrollments = await db.Enrollments.Where(item => item.UserId == user.Id).ToListAsync();
            var remaining = Assert.Single(enrollments);
            Assert.Equal(first.PathId, remaining.PathId);
            Assert.False(remaining.IsCurrent);
        }
    }

    [Fact]
    public async Task LessonMarkdownRequiresEnrollment_AndCurrentPublishedRevision()
    {
        var lesson = await SeedLessonAsync("Current lesson", "# Current lesson body");
        var otherLesson = await SeedLessonAsync("Other lesson", "# Other body");
        var path = await SeedPublishedPathAsync("lesson-path", lesson.LessonId);
        var otherPath = await SeedPublishedPathAsync("other-path");
        var user = await CreateUserAsync();

        using var anonymous = factory.CreateClient();
        var anonymousResponse = await anonymous.GetAsync($"/api/learning-lessons/{lesson.LessonId}");
        Assert.Equal(HttpStatusCode.Forbidden, anonymousResponse.StatusCode);

        using var client = await LoginAsync(user);
        var notEnrolled = await client.GetAsync($"/api/learning-lessons/{lesson.LessonId}");
        Assert.Equal(HttpStatusCode.Forbidden, notEnrolled.StatusCode);

        var join = await client.PostAsync($"/api/learning-paths/{path.PathId}/enroll", null);
        join.EnsureSuccessStatusCode();
        var content = await client.GetAsync($"/api/learning-lessons/{lesson.LessonId}");
        content.EnsureSuccessStatusCode();
        var contentJson = await content.Content.ReadAsStringAsync();
        Assert.Contains("# Current lesson body", contentJson);

        var absentFromCurrentRevision = await client.GetAsync($"/api/learning-lessons/{otherLesson.LessonId}");
        Assert.Equal(HttpStatusCode.NotFound, absentFromCurrentRevision.StatusCode);

        Assert.NotEqual(path.PathId, otherPath.PathId);
    }

    [Fact]
    public async Task CompletingLessonIsIdempotent()
    {
        var lesson = await SeedLessonAsync("Complete me", "# Complete me");
        var path = await SeedPublishedPathAsync("complete-path", lesson.LessonId);
        var user = await CreateUserAsync();
        using var client = await LoginAsync(user);

        var join = await client.PostAsync($"/api/learning-paths/{path.PathId}/enroll", null);
        join.EnsureSuccessStatusCode();

        var first = await client.PostAsync($"/api/learning-lessons/{lesson.LessonId}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var second = await client.PostAsync($"/api/learning-lessons/{lesson.LessonId}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LessonProgress.CountAsync(item =>
            item.UserId == user.Id && item.LessonId == lesson.LessonId));
    }

    private async Task<TestDataSeeder.SeededUser> CreateUserAsync() =>
        await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S06!LearnerPassword");

    private async Task<HttpClient> LoginAsync(TestDataSeeder.SeededUser user)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = user.Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<SeededLesson> SeedLessonAsync(string title, string body)
    {
        var lessonId = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Lessons.Add(new Lesson
        {
            Id = lessonId,
            Localizations = [new LessonLocalization { Locale = "en", Title = title, Body = body }]
        });
        await db.SaveChangesAsync();
        return new SeededLesson(lessonId);
    }

    private async Task<SeededPath> SeedPublishedPathAsync(string name, Guid? lessonId = null)
    {
        var pathId = Guid.CreateVersion7();
        var revisionId = Guid.CreateVersion7();
        var path = new LearningPath
        {
            Id = pathId,
            Slug = $"s06-{name}-{Guid.NewGuid():N}",
            Localizations =
            [new LearningPathLocalization { Locale = "en", Title = name, Summary = "Summary" }]
        };
        var revision = new LearningPathRevision
        {
            Id = revisionId,
            Path = path,
            Status = LearningPathRevisionStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        var module = new LearningModule
        {
            Revision = revision,
            SortOrder = 0,
            Localizations =
            [new LearningModuleLocalization { Locale = "en", Title = "Module", Summary = "Summary" }]
        };
        if (lessonId is { } id)
            module.Items.Add(new ModuleItem { SortOrder = 0, LessonId = id, Module = module });
        revision.Modules.Add(module);
        path.Revisions.Add(revision);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();
        path.CurrentPublishedRevisionId = revisionId;
        await db.SaveChangesAsync();
        return new SeededPath(pathId, path.Slug);
    }

    private sealed record SeededPath(Guid PathId, string Slug);
    private sealed record SeededLesson(Guid LessonId);
}
