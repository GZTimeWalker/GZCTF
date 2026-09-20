using System.Net;
using System.Net.Http.Json;
using GZCTF.Features.ChallengeLibrary.Domain;
using GZCTF.Integration.Test.Base;
using GZCTF.Integration.Test.Fixtures.Challenges;
using GZCTF.Models;
using GZCTF.Models.Request.Account;
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
