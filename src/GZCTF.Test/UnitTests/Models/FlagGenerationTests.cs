using System.Text.RegularExpressions;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

/// <summary>
/// Tests for Challenge flag generation logic
/// </summary>
public class ChallengeFlagGenerationTests
{
    // Static game instance for all tests to avoid repeated key generation
    private static readonly Game TestGame = CreateTestGame();

    // XOR key used for token generation
    private static readonly byte[] TestXorKey = "test_xor_key_1234567890"u8.ToArray();

    private static Game CreateTestGame()
    {
        var game = new Game { Id = 1, Title = "Test Game" };

        // Generate proper key pair with an arbitrary XOR key
        game.GenerateKeyPair(TestXorKey);

        return game;
    }

    private static Challenge CreateChallenge(string? flagTemplate = null) =>
        new() { Id = 1, Title = "Test Challenge", Content = "Test Content", FlagTemplate = flagTemplate };

    private static Participation CreateParticipation(Game game, int teamId = 1, string teamName = "Test Team")
    {
        var team = new Team { Id = teamId, Name = teamName };

        // Generate token using the same method as GameRepository
        var token = $"{team.Id}:{game.Sign($"GZCTF_TEAM_{team.Id}", TestXorKey)}";

        return new Participation
        {
            GameId = game.Id,
            Game = game,
            TeamId = team.Id,
            Team = team,
            Token = token
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GenerateDynamicFlag_WithNullOrEmptyTemplate_ReturnsRandomGuid(string? template)
    {
        // Arrange
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        Assert.EndsWith("}", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithGuidPlaceholder_ReplacesGuid()
    {
        // Arrange
        var challenge = CreateChallenge("MyCTF{[GUID]}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("MyCTF{", flag);
        Assert.EndsWith("}", flag);
        Assert.DoesNotContain("[GUID]", flag);
        Assert.Matches(@"^MyCTF\{[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\}$", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithGuidPlaceholder_DifferentCallsGenerateDifferentFlags()
    {
        // Arrange
        var challenge = CreateChallenge("flag{[GUID]}");

        // Act
        var flag1 = challenge.GenerateDynamicFlag();
        var flag2 = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotEqual(flag1, flag2);
    }

    [Fact]
    public void GenerateDynamicFlag_WithSimpleTemplate_AppliesLeetConversion()
    {
        // Arrange
        var challenge = CreateChallenge("flag{hello world}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        // Verify it was leet converted (should contain converted characters)
        Assert.DoesNotContain("hello world", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
    }

    [Theory]
    [InlineData("[LEET]flag{hello world}")]
    [InlineData("[CLEET]flag{hello sara}")]
    public void GenerateDynamicFlag_WithLeetMarker_AppliesLeetConversion(string template)
    {
        // Arrange
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        Assert.DoesNotContain("[LEET]", flag);
        Assert.DoesNotContain("[CLEET]", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithTeamHashPlaceholder_ReplacesWithHash()
    {
        // Arrange
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge("flag{hello_world_[TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateDynamicFlag(participation);

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{hello_world_", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
        // TEAM_HASH should be 12 characters
        Assert.Matches(@"^flag\{hello_world_[a-f0-9]{12}\}$", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithTeamHashPlaceholder_SameTeamGeneratesSameHash()
    {
        // Arrange
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge("flag{[TEAM_HASH]}");

        // Act
        var flag1 = challenge.GenerateDynamicFlag(participation);
        var flag2 = challenge.GenerateDynamicFlag(participation);

        // Assert
        Assert.Equal(flag1, flag2);
    }

    [Fact]
    public void GenerateDynamicFlag_WithTeamHashPlaceholder_DifferentTeamsGenerateDifferentHashes()
    {
        // Arrange
        var participation1 = CreateParticipation(TestGame, teamId: 1, teamName: "Team 1");
        var participation2 = CreateParticipation(TestGame, teamId: 2, teamName: "Team 2");

        var challenge = CreateChallenge("flag{[TEAM_HASH]}");

        // Act
        var flag1 = challenge.GenerateDynamicFlag(participation1);
        var flag2 = challenge.GenerateDynamicFlag(participation2);

        // Assert
        Assert.NotEqual(flag1, flag2);
    }

    [Theory]
    [InlineData("[LEET]flag{hello world [TEAM_HASH]}")]
    [InlineData("[CLEET]flag{hello sara [TEAM_HASH]}")]
    public void GenerateDynamicFlag_WithLeetMarkerAndTeamHash_AppliesLeetThenTeamHash(string template)
    {
        // Arrange
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag(participation);

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        Assert.DoesNotContain("[LEET]", flag);
        Assert.DoesNotContain("[CLEET]", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
        // Should contain both leet-converted text and team hash (12 hex chars)
        Assert.Matches(@"^flag\{[a-zA-Z0-9_\-!@#$%^&*()]+[a-f0-9]{12}\}$", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_TeamHashShouldNotContainLeetCharacters()
    {
        // Arrange - Create a scenario where hash would be corrupted if Leet was applied
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge("[CLEET]flag{prefix_[TEAM_HASH]_suffix}");

        // Act - Generate twice to verify hash consistency
        var flag1 = challenge.GenerateDynamicFlag(participation);
        var flag2 = challenge.GenerateDynamicFlag(participation);

        // Assert - Extract hash part which should be pure hex (12 chars)
        // Hash is always in the middle, between underscores that surround it
        var match1 = Regex.Match(flag1, @"_([a-f0-9]{12})_");
        var match2 = Regex.Match(flag2, @"_([a-f0-9]{12})_");

        Assert.True(match1.Success && match2.Success, $"Expected hex hash between underscores in: {flag1}, {flag2}");

        // Hash should be identical across calls (deterministic)
        var hash1 = match1.Groups[1].Value;
        var hash2 = match2.Groups[1].Value;
        Assert.Equal(hash1, hash2);

        // Verify hash is pure hex (no leet characters)
        Assert.All(hash1, c => Assert.True("abcdef0123456789".Contains(c), $"Non-hex char '{c}' found in hash"));
    }

    [Fact]
    public void GenerateDynamicFlag_WithLeetAndTeamHash_VerifyHashConsistency()
    {
        // Arrange
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge("[LEET]flag{hello world [TEAM_HASH]}");

        // Act - Generate once to get the team hash
        var flag = challenge.GenerateDynamicFlag(participation);

        // Assert - The hash part should be pure hex and consistent
        var match = Regex.Match(flag, @"\{[^}]*_([a-f0-9]{12})\}");
        Assert.True(match.Success, $"Could not extract hash from: {flag}");

        var hashPart = match.Groups[1].Value;
        Assert.Matches(@"^[a-f0-9]{12}$", hashPart);

        // Generate again and verify the hash is identical (team hash is deterministic)
        var flag2 = challenge.GenerateDynamicFlag(participation);
        var match2 = Regex.Match(flag2, @"\{[^}]*_([a-f0-9]{12})\}");
        Assert.Equal(match.Groups[1].Value, match2.Groups[1].Value);
    }

    [Theory]
    [InlineData("[LEET]flag{test [TEAM_HASH]}")]
    [InlineData("[CLEET]flag{test [TEAM_HASH]}")]
    [InlineData("[LEET]flag{[TEAM_HASH] test}")]
    [InlineData("[CLEET]flag{[TEAM_HASH] test}")]
    public void GenerateDynamicFlag_TeamHashPureHexInAllScenarios(string template)
    {
        // Arrange
        var participation = CreateParticipation(TestGame);
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag(participation);

        // Assert - Hash should always be pure hex, never leet-converted
        var match = Regex.Match(flag, @"\[?[a-f0-9]{12}\]?");
        if (match.Success)
        {
            var hash = match.Value.Trim('[', ']');
            Assert.Matches(@"^[a-f0-9]{12}$", hash);
            // Verify no common leet substitutions exist
            Assert.DoesNotContain("!", hash);
            Assert.DoesNotContain("@", hash);
            Assert.DoesNotContain("#", hash);
            Assert.DoesNotContain("$", hash);
            Assert.DoesNotContain("%", hash);
        }
    }

    [Theory]
    [InlineData(null, "flag{GZCTF_dynamic_flag_test}")]
    [InlineData("", "flag{GZCTF_dynamic_flag_test}")]
    public void GenerateTestFlag_WithNullOrEmptyTemplate_ReturnsTestFlag(string? template, string expected)
    {
        // Arrange
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateTestFlag();

        // Assert
        Assert.Equal(expected, flag);
    }

    [Fact]
    public void GenerateTestFlag_WithGuidPlaceholder_ReplacesGuid()
    {
        // Arrange
        var challenge = CreateChallenge("MyCTF{[GUID]}");

        // Act
        var flag = challenge.GenerateTestFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("MyCTF{", flag);
        Assert.DoesNotContain("[GUID]", flag);
    }

    [Fact]
    public void GenerateTestFlag_WithSimpleTemplate_AppliesLeetConversion()
    {
        // Arrange
        var challenge = CreateChallenge("flag{hello world}");

        // Act
        var flag = challenge.GenerateTestFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
    }

    [Fact]
    public void GenerateTestFlag_WithTeamHashPlaceholder_ReplacesWithTestValue()
    {
        // Arrange
        var challenge = CreateChallenge("flag{[TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateTestFlag();

        // Assert
        Assert.Equal("flag{TestTeamHash}", flag);
    }

    [Fact]
    public void GenerateTestFlag_WithLeetAndTeamHash_NoLeetConversionOnTestTeamHash()
    {
        // Arrange
        var challenge = CreateChallenge("[LEET]flag{hello [TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateTestFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        // TestTeamHash should not be leet-converted (case and content should be preserved)
        Assert.Contains("TestTeamHash", flag);
        Assert.DoesNotContain("[LEET]", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_Parameterless_WithTeamHashPlaceholder_ReplacesWithTestTeamHash()
    {
        // Arrange
        var challenge = CreateChallenge("flag{[TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.Equal("flag{TestTeamHash}", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_Parameterless_WithLeetAndTeamHash_BothApplied()
    {
        // Arrange
        var challenge = CreateChallenge("[LEET]flag{hello [TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{", flag);
        Assert.DoesNotContain("[LEET]", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
    }

    [Theory]
    [InlineData("flag{[GUID]}")]
    [InlineData("[LEET]flag{[GUID]}")]
    [InlineData("[CLEET]flag{[GUID]}")]
    public void GenerateDynamicFlag_WithGuid_PrioritizesGuidReplacement(string template)
    {
        // Arrange
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        // Should not process Leet when [GUID] is present - early return
        Assert.DoesNotContain("[GUID]", flag);
        // The markers should be preserved since we return early on [GUID] detection
        // Actually, according to the logic, [GUID] returns immediately, so markers should be gone
        // Let's verify the flag contains a GUID format
        Assert.Matches(@"\{[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\}", flag);
    }

    [Theory]
    [InlineData("flag{hello}", "flag{")]
    [InlineData("MyCTF{test}", "MyCTF{")]
    [InlineData("custom{payload}", "custom{")]
    public void GenerateDynamicFlag_WithSimpleTemplate_MaintainsFormat(string template, string expectedStart)
    {
        // Arrange
        var challenge = CreateChallenge(template);

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.StartsWith(expectedStart, flag);
        Assert.EndsWith("}", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithMultipleTeamHashPlaceholders_ReplacesAll()
    {
        // Arrange
        var challenge = CreateChallenge("flag{[TEAM_HASH]-[TEAM_HASH]}");
        var part = CreateParticipation(TestGame);

        // Act
        var flag = challenge.GenerateDynamicFlag(part);

        // Assert
        Assert.NotNull(flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.Matches(@"^flag\{[a-f0-9]{12}-[a-f0-9]{12}\}$", flag);

        // Both hashes should be the same
        var parts = flag[5..^1].Split('-');
        Assert.Equal(2, parts.Length);
        Assert.Equal(parts[0], parts[1]);
    }

    [Fact]
    public void GenerateDynamicFlag_WithLeetAndMultipleTeamHash_PreservesAllTeamHash()
    {
        // Arrange
        var challenge = CreateChallenge("[LEET]flag{AAA [TEAM_HASH] BBB [TEAM_HASH] CCC}");
        var part = CreateParticipation(TestGame);

        // Act
        var flag = challenge.GenerateDynamicFlag(part);

        // Assert
        Assert.NotNull(flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.StartsWith("flag{", flag);
        Assert.EndsWith("}", flag);

        // Extract the two hashes and verify they are the same
        var hashPattern = @"[a-f0-9]{12}";
        var matches = Regex.Matches(flag, hashPattern);
        Assert.Equal(2, matches.Count);
        Assert.Equal(matches[0].Value, matches[1].Value);
    }

    [Fact]
    public void GenerateDynamicFlag_WithCLeetAndMultipleTeamHash_PreservesAllTeamHash()
    {
        // Arrange
        var challenge = CreateChallenge("[CLEET]flag{XXX [TEAM_HASH] YYY [TEAM_HASH] ZZZ}");
        var part = CreateParticipation(TestGame);

        // Act
        var flag = challenge.GenerateDynamicFlag(part);

        // Assert
        Assert.NotNull(flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);

        // Extract the two hashes and verify they are the same
        var hashPattern = @"[a-f0-9]{12}";
        var matches = Regex.Matches(flag, hashPattern);
        Assert.Equal(2, matches.Count);
        Assert.Equal(matches[0].Value, matches[1].Value);
    }

    [Fact]
    public void GenerateDynamicFlag_WithNestedBracesAndPlaceholders_HandlesCorrectly()
    {
        // Arrange
        var challenge = CreateChallenge("flag{{hello}}_{[GUID]}_{[TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.NotNull(flag);
        Assert.StartsWith("flag{{hello}}_", flag);
        Assert.DoesNotContain("[GUID]", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.EndsWith("}_{TestTeamHash}", flag);
        // Should contain a GUID pattern in the middle
        var guidPattern = @"\{[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\}";
        Assert.Matches(guidPattern, flag);

        // Assert parse result
        var generator = new DynamicFlagGenerator("flag{{hello}}_{[GUID]a}_{[TEAM_HASH]} ");
        var template = generator.FlagTemplate;
        Assert.Equal(LeetMode.None, template.LeetMode);
        Assert.Equal(9, template.Segments.Count);
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[0].Type); // flag{
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[1].Type); // {hello}}
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[2].Type); // _{
        Assert.Equal(FlagSegmentType.Guid, template.Segments[3].Type); // [GUID]
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[4].Type); // a}
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[5].Type); // _{
        Assert.Equal(FlagSegmentType.TeamHash, template.Segments[6].Type); // [TEAM_HASH]
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[7].Type); // }
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[8].Type); // (trailing space)
    }

    [Fact]
    public void GenerateDynamicFlag_Parameterless_WithMultipleTeamHash_UsesTestTeamHash()
    {
        // Arrange
        var challenge = CreateChallenge("flag{[TEAM_HASH]-[TEAM_HASH]-[TEAM_HASH]}");

        // Act
        var flag = challenge.GenerateDynamicFlag();

        // Assert
        Assert.Equal("flag{TestTeamHash-TestTeamHash-TestTeamHash}", flag);
    }

    [Fact]
    public void GenerateDynamicFlag_WithNestedBraces_HandlesCorrectly()
    {
        // Arrange
        var challenge = CreateChallenge("[LEET]flag{leet {{braces}} [TEAM_HASH]} {leet} 666");
        var part = CreateParticipation(TestGame);

        // Act
        var flag = challenge.GenerateDynamicFlag(part);

        // Assert
        Assert.NotNull(flag);
        Assert.DoesNotContain("[LEET]", flag);
        Assert.DoesNotContain("[TEAM_HASH]", flag);
        Assert.DoesNotContain("[", flag);
        Assert.DoesNotContain("]", flag);
        // Should start with "flag{" (plain)
        Assert.StartsWith("flag{", flag);
        Assert.EndsWith("} 666", flag);
        // The team hash should be pure hex
        var hashMatch = Regex.Match(flag, @"([a-f0-9]{12})");
        Assert.True(hashMatch.Success, $"No 12-char hex hash found in: {flag}");
        Assert.All(hashMatch.Groups[1].Value,
            c => Assert.True("abcdef0123456789".Contains(c), $"Non-hex char '{c}' in hash"));

        // Assert parse result
        var generator = new DynamicFlagGenerator("[LEET]flag{leet {{braces}} [TEAM_HASH]} {leet} 666");
        var template = generator.FlagTemplate;
        Assert.Equal(LeetMode.Leet, template.LeetMode);
        Assert.Equal(7, template.Segments.Count);
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[0].Type); // "flag{"
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[1].Type); // "leet {{braces}} "
        Assert.Equal(FlagSegmentType.TeamHash, template.Segments[2].Type); // "[TEAM_HASH]"
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[3].Type); // "}"
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[4].Type); // " {"
        Assert.Equal(FlagSegmentType.LeetText, template.Segments[5].Type); // "leet}"
        Assert.Equal(FlagSegmentType.PlainText, template.Segments[6].Type); // " 666"
    }
}
