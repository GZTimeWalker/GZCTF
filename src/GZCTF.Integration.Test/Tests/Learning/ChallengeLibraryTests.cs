using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Features.Imports.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.LearningProgress.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using UserRole = GZCTF.Utils.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Learning;

[Collection(nameof(IntegrationTestCollection))]
public class ChallengeLibraryTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task ChallengeCtfCategory_RoundTripsThroughCreateUpdateAndList()
    {
        using var admin = await CreateAdminClientAsync();
        var created = await admin.PostAsJsonAsync("/api/admin/challenges", new
        {
            type = "StaticAttachment",
            ctfCategory = "Web",
            localizations = new[] { new { locale = "en", title = "Category test", summary = "", body = "Body" } },
            flags = new[] { new { kind = "Static", value = "flag{category}" } }
        });
        created.EnsureSuccessStatusCode();
        var challengeId = ReadGuid(await created.Content.ReadAsStringAsync());
        var createdJson = JsonNode.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal("Web", createdJson?["challenge"]?["ctfCategory"]?.GetValue<string>());

        var updated = await admin.PutAsJsonAsync($"/api/admin/challenges/{challengeId}", new
        {
            ctfCategory = "Misc"
        });
        updated.EnsureSuccessStatusCode();
        var updatedJson = JsonNode.Parse(await updated.Content.ReadAsStringAsync());
        Assert.Equal("Misc", updatedJson?["challenge"]?["ctfCategory"]?.GetValue<string>());

        var listed = JsonNode.Parse(await admin.GetStringAsync("/api/admin/challenges"))?.AsArray();
        Assert.Contains(listed!, item =>
            item?["id"]?.GetValue<string>() == challengeId.ToString() &&
            item?["ctfCategory"]?.GetValue<string>() == "Misc");
    }

    [Fact]
    public async Task AdministratorChallengeAndLessonCrud_ProtectsSecretsAndFallsBackToEnglish()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/admin/challenges")).StatusCode);

        using var admin = await CreateAdminClientAsync();
        var challengeResponse = await admin.PostAsJsonAsync("/api/admin/challenges", new
        {
            type = "StaticAttachment",
            sourceType = "native",
            sourceId = $"s04-{Guid.NewGuid():N}",
            localizations = new[]
            {
                new { locale = "en", title = "English title", summary = "English summary", body = "English body" },
                new { locale = "zh-CN", title = "中文标题", summary = "中文摘要", body = "中文正文" }
            },
            flags = new[] { new { kind = "Static", value = "flag{sensitive}" } },
            hints = new[] { new { locale = "en", sortOrder = 0, content = "Hint" } },
            writeups = new[] { new { locale = "en", content = "WP" } }
        });
        challengeResponse.EnsureSuccessStatusCode();
        var challengeId = ReadGuid(await challengeResponse.Content.ReadAsStringAsync());

        var listBody = await (await admin.GetAsync("/api/admin/challenges?locale=fr-FR"))
            .Content.ReadAsStringAsync();
        Assert.DoesNotContain("flag{sensitive}", listBody);
        Assert.Contains("English title", listBody);

        var editResponse = await admin.GetAsync($"/api/admin/challenges/{challengeId}/edit?locale=fr-FR");
        editResponse.EnsureSuccessStatusCode();
        Assert.Contains("no-store", editResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        var editBody = await editResponse.Content.ReadAsStringAsync();
        Assert.Contains("flag{sensitive}", editBody);
        Assert.Contains("English body", editBody);

        var updateResponse = await admin.PutAsJsonAsync($"/api/admin/challenges/{challengeId}", new
        {
            localizations = new[]
            {
                new { locale = "en", title = "Updated title", summary = "Updated summary", body = "Updated body" }
            }
        });
        updateResponse.EnsureSuccessStatusCode();

        var lessonResponse = await admin.PostAsJsonAsync("/api/admin/lessons", new
        {
            localizations = new[]
            {
                new { locale = "en", title = "Lesson", body = "# Markdown lesson" },
                new { locale = "zh-CN", title = "课节", body = "# 中文课节" }
            }
        });
        lessonResponse.EnsureSuccessStatusCode();
        var lessonId = ReadGuid(await lessonResponse.Content.ReadAsStringAsync());

        var lessonBody = await (await admin.GetAsync($"/api/admin/lessons/{lessonId}?locale=fr-FR"))
            .Content.ReadAsStringAsync();
        Assert.Contains("# Markdown lesson", lessonBody);

        var lessonUpdate = await admin.PutAsJsonAsync($"/api/admin/lessons/{lessonId}", new
        {
            localizations = new[]
            {
                new { locale = "en", title = "Updated lesson", body = "# Updated markdown" }
            }
        });
        lessonUpdate.EnsureSuccessStatusCode();

        var lessonDelete = await admin.DeleteAsync($"/api/admin/lessons/{lessonId}");
        Assert.Equal(HttpStatusCode.NoContent, lessonDelete.StatusCode);
    }

    [Fact]
    public async Task PublishedChallengeType_IsImmutable()
    {
        var challengeId = Guid.CreateVersion7();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Challenges.Add(new Challenge
            {
                Id = challengeId,
                Type = ChallengeType.StaticAttachment,
                PublicationState = ChallengePublicationState.Published,
                SourceType = "native",
                SourceId = $"published-{Guid.NewGuid():N}",
                Localizations =
                [
                    new ChallengeLocalization
                    {
                        Locale = "en",
                        Title = "Published",
                        Summary = "Published",
                        Body = "Published body"
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        using var admin = await CreateAdminClientAsync();
        var response = await admin.PutAsJsonAsync($"/api/admin/challenges/{challengeId}", new
        {
            type = "DynamicContainer"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Merge_RedirectsReferencesKeepsEarliestProgressAndPreservesMappings()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "Test@123");
        var survivorId = Guid.CreateVersion7();
        var duplicateId = Guid.CreateVersion7();
        var pathId = Guid.CreateVersion7();
        var revisionId = Guid.CreateVersion7();
        var moduleId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var sourceSuffix = Guid.NewGuid().ToString("N");
        var survivorSourceId = $"survivor-{sourceSuffix}";
        var duplicateSourceId = $"duplicate-{sourceSuffix}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var survivor = new Challenge
            {
                Id = survivorId,
                Type = ChallengeType.StaticAttachment,
                SourceType = "legacy",
                SourceId = survivorSourceId,
                Localizations =
                [
                    new ChallengeLocalization
                    {
                        Locale = "en",
                        Title = "Survivor",
                        Summary = "Survivor",
                        Body = "Body"
                    }
                ]
            };
            var duplicate = new Challenge
            {
                Id = duplicateId,
                Type = ChallengeType.StaticAttachment,
                SourceType = "legacy",
                SourceId = duplicateSourceId,
                Localizations =
                [
                    new ChallengeLocalization
                    {
                        Locale = "en",
                        Title = "Duplicate",
                        Summary = "Duplicate",
                        Body = "Body"
                    }
                ]
            };
            var path = new LearningPath { Id = pathId, Slug = $"merge-{Guid.NewGuid():N}" };
            var revision = new LearningPathRevision { Id = revisionId, Path = path };
            var module = new LearningModule { Id = moduleId, Revision = revision, SortOrder = 0 };
            module.Items.Add(new ModuleItem { Id = itemId, SortOrder = 0, Challenge = duplicate });
            revision.Modules.Add(module);

            db.AddRange(survivor, duplicate, path, revision);
            db.ChallengeProgress.AddRange(
                new ChallengeProgress
                {
                    UserId = user.Id,
                    Challenge = survivor,
                    SolvedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
                },
                new ChallengeProgress
                {
                    UserId = user.Id,
                    Challenge = duplicate,
                    SolvedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                });
            db.LearnerDailySolveStats.AddRange(
                new LearnerDailySolveStat
                {
                    UserId = user.Id,
                    Date = new DateOnly(2026, 1, 1),
                    SolveCount = 1
                },
                new LearnerDailySolveStat
                {
                    UserId = user.Id,
                    Date = new DateOnly(2026, 1, 2),
                    SolveCount = 1
                });
            db.LegacyChallengeMaps.AddRange(
                new LegacyChallengeMap
                {
                    SourceType = "legacy",
                    SourceId = survivorSourceId,
                    Challenge = survivor
                },
                new LegacyChallengeMap
                {
                    SourceType = "legacy",
                    SourceId = duplicateSourceId,
                    Challenge = duplicate
                });
            await db.SaveChangesAsync();
        }

        using var admin = await CreateAdminClientAsync();
        var mergeResponse = await admin.PostAsJsonAsync($"/api/admin/challenges/{duplicateId}/merge", new
        {
            survivorId
        });
        mergeResponse.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(survivorId, await db.ModuleItems
                .Where(item => item.Id == itemId)
                .Select(item => item.ChallengeId)
                .SingleAsync());

            var progress = await db.ChallengeProgress
                .Where(item => item.UserId == user.Id)
                .SingleAsync();
            Assert.Equal(survivorId, progress.ChallengeId);
            Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), progress.SolvedAtUtc);

            var dailyStats = await db.LearnerDailySolveStats
                .Where(item => item.UserId == user.Id)
                .OrderBy(item => item.Date)
                .ToListAsync();
            var stat = Assert.Single(dailyStats);
            Assert.Equal(new DateOnly(2026, 1, 1), stat.Date);
            Assert.Equal(1, stat.SolveCount);

            Assert.Equal(2, await db.LegacyChallengeMaps.CountAsync(item =>
                item.ChallengeId == survivorId || item.ChallengeId == duplicateId));
            Assert.Equal(ChallengePublicationState.Merged,
                await db.Challenges.Where(item => item.Id == duplicateId)
                    .Select(item => item.PublicationState)
                    .SingleAsync());
        }
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var password = "S04!AdminPassword";
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: UserRole.Admin);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    private static Guid ReadGuid(string json)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Expected JSON response");
        var value = node["id"]?.GetValue<string>()
                    ?? node["data"]?["id"]?.GetValue<string>()
                    ?? node["challenge"]?["id"]?.GetValue<string>();
        return Guid.Parse(value ?? throw new InvalidOperationException("Expected id in response"));
    }
}
