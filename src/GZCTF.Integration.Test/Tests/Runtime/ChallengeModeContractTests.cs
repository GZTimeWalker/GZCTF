using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Integration.Test.Fixtures.Challenges;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
using GZCTF.Storage.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Runtime;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ChallengeModeContractTests(GZCTFApplicationFactory factory)
{
    public static IEnumerable<object[]> Fixtures() =>
        ChallengeModeFixtures.All.Select(fixture => new object[] { fixture });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Learner_detail_exposes_the_published_mode_contract(ChallengeModeFixture fixture)
    {
        var challengeId = await SeedFixtureAsync(fixture);
        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S08!LearnerPassword");

        using var anonymous = factory.CreateClient();
        var anonymousResponse = await anonymous.GetAsync($"/api/challenges/{challengeId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var learner = factory.CreateClient();
        var login = await learner.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = user.Password });
        login.EnsureSuccessStatusCode();

        var response = await learner.GetAsync($"/api/challenges/{challengeId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(fixture.Title, body);
        Assert.Contains(fixture.Type.ToString(), body);
        Assert.DoesNotContain(fixture.StaticFlag ?? "flag{", body);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Anonymous_users_cannot_download_start_or_submit(ChallengeModeFixture fixture)
    {
        var challengeId = await SeedFixtureAsync(fixture);
        using var anonymous = factory.CreateClient();

        var download = await anonymous.GetAsync($"/api/challenges/{challengeId}/attachment");
        var start = await anonymous.PostAsync($"/api/challenges/{challengeId}/instances", null);
        var submit = await anonymous.PostAsJsonAsync($"/api/challenges/{challengeId}/submissions",
            new { flag = fixture.StaticFlag ?? fixture.Attachments.FirstOrDefault()?.Flag ?? "flag{unknown}" });

        Assert.Equal(HttpStatusCode.Unauthorized, download.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, start.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, submit.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Fixture_records_every_mode_specific_runtime_invariant(ChallengeModeFixture fixture)
    {
        Assert.False(string.IsNullOrWhiteSpace(fixture.Title));

        if (fixture.IsAttachment)
            Assert.NotEmpty(fixture.Attachments);
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(fixture.ContainerImage));
            Assert.InRange(fixture.ExposedPort, 1, 65535);
            Assert.True(fixture.Cpu > 0);
            Assert.True(fixture.MemoryMb > 0);
            Assert.True(fixture.StorageMb > 0);
            Assert.False(string.IsNullOrWhiteSpace(fixture.NetworkMode));
        }

        if (fixture.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer)
            Assert.False(string.IsNullOrWhiteSpace(fixture.StaticFlag));
        else if (fixture.Type == ChallengeType.DynamicContainer)
            Assert.False(string.IsNullOrWhiteSpace(fixture.FlagTemplate));
        else
            Assert.All(fixture.Attachments, item => Assert.False(string.IsNullOrWhiteSpace(item.Flag)));
    }

    [Fact]
    public async Task Dynamic_attachment_is_reserved_once_per_learner_and_static_is_stable()
    {
        var dynamicFixture = ChallengeModeFixtures.All.Single(item => item.IsDynamic && item.IsAttachment);
        var staticFixture = ChallengeModeFixtures.All.Single(item => !item.IsDynamic && item.IsAttachment);
        var dynamicChallengeId = await SeedFixtureAsync(dynamicFixture);
        var staticChallengeId = await SeedFixtureAsync(staticFixture);
        var first = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S10!LearnerPassword");
        var second = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), "S10!LearnerPassword");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserChallengeInstances.AddRange(
                new UserChallengeInstance { UserId = first.Id, ChallengeId = dynamicChallengeId },
                new UserChallengeInstance { UserId = second.Id, ChallengeId = dynamicChallengeId },
                new UserChallengeInstance { UserId = first.Id, ChallengeId = staticChallengeId });
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var allocator = scope.ServiceProvider.GetRequiredService<DynamicAttachmentAllocator>();
            var firstAssignment = await allocator.GetOrAllocateAsync(first.Id, dynamicChallengeId);
            var secondAssignment = await allocator.GetOrAllocateAsync(second.Id, dynamicChallengeId);
            var repeated = await allocator.GetOrAllocateAsync(first.Id, dynamicChallengeId);
            var staticAssignment = await allocator.GetOrAllocateAsync(first.Id, staticChallengeId);

            Assert.NotEqual(firstAssignment.Sha256, secondAssignment.Sha256);
            Assert.Equal(firstAssignment, repeated);
            Assert.Equal(staticFixture.Attachments[0].Sha256, staticAssignment.Sha256);
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Every_mode_solves_exactly_once_through_the_skill_tree_workspace(ChallengeModeFixture fixture)
    {
        // 1. Publish the challenge into an active category of a published tree.
        var (treeId, challengeId) = await SeedPublishedTreeAsync(fixture);
        var (user, client) = await CreateLearnerClientAsync();

        // 2. Enroll the learner.
        (await client.PostAsync($"/api/skill-tree-enrollments/{treeId}", null)).EnsureSuccessStatusCode();

        // 3. Fetch it through the skill tree workspace route.
        var workspace = await client.GetStringAsync($"/api/skill-trees/{treeId}");
        Assert.Contains(challengeId.ToString(), workspace);
        Assert.Contains(fixture.Title, workspace);

        // 4. Download or allocate its resource. Attachment modes serve a real file; container
        //    modes expose the runtime contract because the fixture images are not published.
        //    A dynamic container records its per-learner flag when the container starts, so the
        //    assignment row is seeded here in place of an unavailable Docker runtime.
        if (fixture.IsAttachment)
            (await client.GetAsync($"/api/challenges/{challengeId}/attachment")).EnsureSuccessStatusCode();
        else
        {
            // The learner detail deliberately hides the container runtime configuration;
            // assert the workspace exposes the mode contract instead.
            var detail = await client.GetStringAsync($"/api/challenges/{challengeId}");
            Assert.Contains(fixture.Type.ToString(), detail);
            Assert.Contains("\"hasContainer\":true", detail);
            Assert.Contains("\"hasAttachment\":false", detail);
        }

        // 5. Submit the correct Flag.
        var flag = await ResolveExpectedFlagAsync(fixture, challengeId, user.Id);
        if (fixture.Type == ChallengeType.DynamicContainer)
            await SeedContainerAssignmentAsync(user.Id, challengeId, flag);

        (await client.PostAsJsonAsync($"/api/challenges/{challengeId}/submissions", new { flag }))
            .EnsureSuccessStatusCode();

        // 6. Assert one ChallengeProgress row.
        Assert.Equal(1, await CountProgressRowsAsync(challengeId, user.Id));

        // 7. Re-submit and assert the count remains one.
        (await client.PostAsJsonAsync($"/api/challenges/{challengeId}/submissions", new { flag }))
            .EnsureSuccessStatusCode();
        Assert.Equal(1, await CountProgressRowsAsync(challengeId, user.Id));
    }

    private async Task<string> ResolveExpectedFlagAsync(
        ChallengeModeFixture fixture, Guid challengeId, Guid userId)
    {
        if (fixture.Type is ChallengeType.StaticAttachment or ChallengeType.StaticContainer)
            return fixture.ExpectedStaticFlag;

        if (fixture.Type == ChallengeType.DynamicContainer)
            return fixture.FlagTemplate!.Replace("{userId}", userId.ToString("N"), StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assigned = await db.UserChallengeInstances.AsNoTracking()
            .Where(item => item.UserId == userId && item.ChallengeId == challengeId && item.IsActive)
            .Select(item => item.AssignedAttachmentSha256)
            .SingleAsync();
        return fixture.Attachments.Single(item => item.Sha256 == assigned).Flag;
    }

    private async Task SeedContainerAssignmentAsync(Guid userId, Guid challengeId, string flag)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserChallengeInstances.Add(new UserChallengeInstance
        {
            UserId = userId,
            ChallengeId = challengeId,
            Status = ChallengeInstanceStatus.Running,
            IsActive = true,
            StartedAtUtc = DateTimeOffset.UtcNow,
            AssignedFlag = flag
        });
        await db.SaveChangesAsync();
    }

    private async Task<int> CountProgressRowsAsync(Guid challengeId, Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ChallengeProgress.CountAsync(item => item.ChallengeId == challengeId && item.UserId == userId);
    }

    private async Task<(TestDataSeeder.SeededUser User, HttpClient Client)> CreateLearnerClientAsync()
    {
        const string password = "S27!LearnerPassword";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        login.EnsureSuccessStatusCode();
        return (user, client);
    }

    private async Task<(Guid TreeId, Guid ChallengeId)> SeedPublishedTreeAsync(ChallengeModeFixture fixture)
    {
        var challengeId = Guid.CreateVersion7();
        var treeId = Guid.CreateVersion7();
        var revisionId = Guid.CreateVersion7();
        var categoryId = Guid.CreateVersion7();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = challengeId,
            Type = fixture.Type,
            PublicationState = ChallengePublicationState.Published,
            IsEnabled = true,
            SourceType = "s27-mode",
            SourceId = $"{fixture.Key}-{challengeId:N}",
            RuntimeConfigurationJson = JsonSerializer.Serialize(fixture),
            Localizations =
            [
                new ChallengeLocalization
                {
                    Locale = "en",
                    Title = fixture.Title,
                    Summary = $"{fixture.Type} fixture",
                    Body = "Protected challenge body"
                }
            ],
            Flags = fixture.Type switch
            {
                ChallengeType.StaticAttachment or ChallengeType.StaticContainer =>
                    [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = fixture.StaticFlag }],
                ChallengeType.DynamicContainer =>
                [
                    new ChallengeFlag
                    {
                        Kind = ChallengeFlagKind.Template, Template = fixture.FlagTemplate
                    }
                ],
                _ =>
                [
                    new ChallengeFlag
                    {
                        Kind = ChallengeFlagKind.DynamicAttachment,
                        AttachmentPoolKey = fixture.Key,
                        MetadataJson = JsonSerializer.Serialize(fixture.Attachments)
                    }
                ]
            }
        });

        var category = new SkillCategory
        {
            Id = categoryId,
            Name = $"Workspace category {categoryId:N}",
            Summary = "",
            IconKey = "flag"
        };
        category.Contents.Add(new CategoryContent
        {
            Id = Guid.CreateVersion7(),
            CategoryId = categoryId,
            ChallengeId = challengeId,
            SortOrder = 0
        });

        var revision = new SkillTreeRevision
        {
            Id = revisionId,
            SkillTreeId = treeId,
            Status = SkillTreeRevisionStatus.Published,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        revision.Categories.Add(new SkillTreeCategoryRef
        {
            Id = Guid.CreateVersion7(),
            RevisionId = revisionId,
            CategoryId = categoryId,
            Category = category,
            SortOrder = 0
        });

        db.SkillTrees.Add(new SkillTree
        {
            Id = treeId,
            Name = $"Workspace tree {treeId:N}",
            Summary = "",
            IconKey = "web",
            Revisions = [revision]
        });
        await db.SaveChangesAsync();

        // Attachment modes must serve a real file, so seed the pool into blob storage.
        var storage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();
        foreach (var attachment in fixture.Attachments)
        {
            var bytes = Encoding.UTF8.GetBytes($"payload for {attachment.FileName}");
            using var stream = new MemoryStream(bytes);
            await storage.WriteAsync(attachment.FileName, stream);
        }

        // Point the tree at the published revision after the graph exists to avoid a
        // circular dependency between the new tree and its own revision.
        await db.SkillTrees.Where(item => item.Id == treeId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.CurrentPublishedRevisionId, revisionId));

        return (treeId, challengeId);
    }

    private async Task<Guid> SeedFixtureAsync(ChallengeModeFixture fixture)
    {
        var challengeId = Guid.CreateVersion7();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Challenges.Add(new Challenge
        {
            Id = challengeId,
            Type = fixture.Type,
            PublicationState = ChallengePublicationState.Published,
            SourceType = "s08-fixture",
            SourceId = $"{fixture.Key}-{challengeId:N}",
            RuntimeConfigurationJson = System.Text.Json.JsonSerializer.Serialize(fixture),
            Localizations =
            [
                new ChallengeLocalization
                {
                    Locale = "en",
                    Title = fixture.Title,
                    Summary = $"{fixture.Type} fixture",
                    Body = "Protected challenge body"
                }
            ],
            Flags = fixture.Type switch
            {
                ChallengeType.StaticAttachment or ChallengeType.StaticContainer =>
                [new ChallengeFlag { Kind = ChallengeFlagKind.Static, Value = fixture.StaticFlag }],
                ChallengeType.DynamicContainer =>
                [new ChallengeFlag { Kind = ChallengeFlagKind.Template, Template = fixture.FlagTemplate }],
                _ => [new ChallengeFlag
                {
                    Kind = ChallengeFlagKind.DynamicAttachment,
                    AttachmentPoolKey = fixture.Key,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(fixture.Attachments)
                }]
            }
        });
        await db.SaveChangesAsync();
        return challengeId;
    }
}
