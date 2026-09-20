using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.LearningPaths.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.SkillTrees;

[Collection(nameof(IntegrationTestCollection))]
public class SkillTreeSchemaTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task Tree_revision_has_unique_category_and_sort_order()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var category = new SkillCategory { Name = "Web", IconKey = "web" };
            var revision = new SkillTreeRevision
            {
                SkillTree = new SkillTree { Name = "Beginner", IconKey = "flag" },
                Status = SkillTreeRevisionStatus.Draft
            };
            revision.Categories.Add(new SkillTreeCategoryRef { Category = category, SortOrder = 0 });
            revision.Categories.Add(new SkillTreeCategoryRef { Category = category, SortOrder = 1 });
            db.SkillTreeRevisions.Add(revision);
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Category_content_references_exactly_one_content_type()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            db.CategoryContents.Add(new CategoryContent
            {
                Category = new SkillCategory { Name = "Web", IconKey = "web" },
                Lesson = new Lesson(),
                Challenge = new Challenge { Type = ChallengeType.StaticAttachment },
                SortOrder = 0
            });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task User_has_at_most_one_current_skill_tree()
    {
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "SkillTree!123");
        await AssertConstraintViolationAsync(async db =>
        {
            db.SkillTreeEnrollments.AddRange(
                new SkillTreeEnrollment
                {
                    UserId = user.Id,
                    SkillTree = new SkillTree { Name = "Web", IconKey = "web" },
                    IsCurrent = true
                },
                new SkillTreeEnrollment
                {
                    UserId = user.Id,
                    SkillTree = new SkillTree { Name = "AI", IconKey = "ai" },
                    IsCurrent = true
                });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Category_cannot_contain_the_same_challenge_twice()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var category = new SkillCategory { Name = "Web", IconKey = "web" };
            var challenge = new Challenge { Type = ChallengeType.StaticAttachment };
            category.Contents.Add(new CategoryContent { Challenge = challenge, SortOrder = 0 });
            category.Contents.Add(new CategoryContent { Challenge = challenge, SortOrder = 1 });
            db.SkillCategories.Add(category);
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Tree_has_at_most_one_draft_revision()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var tree = new SkillTree { Name = "Web", IconKey = "web" };
            tree.Revisions.Add(new SkillTreeRevision { Status = SkillTreeRevisionStatus.Draft });
            tree.Revisions.Add(new SkillTreeRevision { Status = SkillTreeRevisionStatus.Draft });
            db.SkillTrees.Add(tree);
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Old_slug_redirect_is_unique()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            var tree = new SkillTree { Name = "Web", IconKey = "web" };
            db.LearningPathRedirects.AddRange(
                new LearningPathRedirect { LearningPathId = Guid.CreateVersion7(), OldSlug = "old-web", SkillTree = tree },
                new LearningPathRedirect { LearningPathId = Guid.CreateVersion7(), OldSlug = "old-web", SkillTree = tree });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Unknown_icon_key_is_rejected_by_database()
    {
        await AssertConstraintViolationAsync(async db =>
        {
            db.SkillTrees.Add(new SkillTree { Name = "Web", IconKey = "arbitrary" });
            await db.SaveChangesAsync();
        });
    }

    private async Task AssertConstraintViolationAsync(Func<AppDbContext, Task> arrangeAndSave)
    {
        using var scope = factory.Services.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => arrangeAndSave(db));
        await transaction.RollbackAsync();
    }
}
