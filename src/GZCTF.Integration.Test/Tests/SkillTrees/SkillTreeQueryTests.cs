using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillTreeQueryTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Anonymous_list_returns_only_active_published_trees_without_progress()
    {
        await SeedAsync();

        using var client = factory.CreateClient();
        var response = await client.GetFromJsonAsync<SkillTreeSummaryResponse[]>("/api/skill-trees");

        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.All(response, tree =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(tree);
            Assert.DoesNotContain("progress", json, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(Guid.Empty, tree.SkillTreeId);
        });
        Assert.Contains(response, tree => tree.Name == "Empty published tree" && tree.CategoryCount == 0);
        Assert.DoesNotContain(response, tree => tree.Name is "Draft tree" or "Deleted tree");
    }

    [Fact]
    public async Task Anonymous_detail_excludes_draft_content_and_protected_fields()
    {
        var publishedTreeId = await SeedAsync();

        using var client = factory.CreateClient();
        var json = await client.GetStringAsync($"/api/skill-trees/{publishedTreeId}");

        Assert.DoesNotContain("\"flags\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("flagValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("writeup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Draft lesson", json, StringComparison.Ordinal);
        Assert.Contains("Published lesson", json, StringComparison.Ordinal);
    }

    private async Task<Guid> SeedAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var publishedChallenge = new Challenge
        {
            Id = Guid.CreateVersion7(),
            Type = ChallengeType.StaticAttachment,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true,
            Localizations =
            [
                new ChallengeLocalization { Locale = "zh-CN", Title = "已发布题目", Summary = "题目简介" },
                new ChallengeLocalization { Locale = "en", Title = "Published challenge", Summary = "Summary" }
            ]
        };

        var draftLesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Draft,
            Localizations =
            [
                new LessonLocalization { Locale = "zh-CN", Title = "草稿课节", Body = "正文" }
            ]
        };

        var publishedLesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            PublicationState = LessonPublicationState.Published,
            Localizations =
            [
                new LessonLocalization { Locale = "zh-CN", Title = "已发布课节", Body = "正文" },
                new LessonLocalization { Locale = "en", Title = "Published lesson", Body = "Body" }
            ]
        };

        var emptyTree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Empty published tree",
            Summary = "空技能树",
            IconKey = "flag",
            Revisions = []
        };
        var emptyRevision = new SkillTreeRevision
        {
            Id = Guid.CreateVersion7(),
            Status = SkillTreeRevisionStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        emptyTree.Revisions.Add(emptyRevision);

        var populatedTree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Populated tree",
            Summary = "有内容的技能树",
            IconKey = "web",
            Revisions = []
        };
        var populatedRevision = new SkillTreeRevision
        {
            Id = Guid.CreateVersion7(),
            Status = SkillTreeRevisionStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        var category = new SkillCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Web",
            Summary = "Web 类别",
            IconKey = "web"
        };
        category.Contents.Add(new CategoryContent { Category = category, CategoryId = category.Id, Challenge = publishedChallenge, ChallengeId = publishedChallenge.Id, SortOrder = 0 });
        category.Contents.Add(new CategoryContent { Category = category, CategoryId = category.Id, Lesson = publishedLesson, LessonId = publishedLesson.Id, SortOrder = 1 });
        populatedRevision.Categories.Add(new SkillTreeCategoryRef { Category = category, Revision = populatedRevision, SortOrder = 0 });
        populatedTree.Revisions.Add(populatedRevision);

        var draftTree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Draft tree",
            Summary = "草稿",
            IconKey = "pwn",
            Revisions =
            [
                new SkillTreeRevision
                {
                    Id = Guid.CreateVersion7(),
                    Status = SkillTreeRevisionStatus.Draft,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                }
            ]
        };

        var deletedTree = new SkillTree
        {
            Id = Guid.CreateVersion7(),
            Name = "Deleted tree",
            Summary = "已删除",
            IconKey = "brain",
            DeletedAtUtc = DateTimeOffset.UtcNow,
            Revisions =
            [
                new SkillTreeRevision
                {
                    Id = Guid.CreateVersion7(),
                    Status = SkillTreeRevisionStatus.Published,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    PublishedAtUtc = DateTimeOffset.UtcNow
                }
            ]
        };

        db.SkillTrees.AddRange(emptyTree, populatedTree, draftTree, deletedTree);
        db.Challenges.Add(publishedChallenge);
        db.Lessons.AddRange(draftLesson, publishedLesson);
        await db.SaveChangesAsync();

        await using var scope2 = factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        await db2.SkillTrees.Where(t => t.Id == emptyTree.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CurrentPublishedRevisionId, emptyRevision.Id));
        await db2.SkillTrees.Where(t => t.Id == populatedTree.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CurrentPublishedRevisionId, populatedRevision.Id));
        await db2.SkillTrees.Where(t => t.Id == deletedTree.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CurrentPublishedRevisionId, deletedTree.Revisions[0].Id));

        return populatedTree.Id;
    }
}
