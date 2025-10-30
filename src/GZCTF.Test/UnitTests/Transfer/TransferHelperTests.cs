using System;
using GZCTF.Models.Transfer;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Transfer;

public class TransferHelperTests(ITestOutputHelper output)
{
    [Fact]
    public void ToToml_SerializeObject_Success()
    {
        // Arrange
        var game = new TransferGame
        {
            Title = "Test Game",
            Summary = "Test Summary",
            Content = "Test Content",
            Hidden = false,
            PracticeMode = true,
            StartTime = new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 10, 3, 0, 0, 0, TimeSpan.Zero),
            BloodBonus = new BloodBonusSection
            {
                First = 50,
                Second = 30,
                Third = 10
            }
        };

        // Act
        var toml = TransferHelper.ToToml(game);

        output.WriteLine(toml);

        // Assert
        Assert.NotNull(toml);
        Assert.NotEmpty(toml);
        Assert.Contains("title = \"Test Game\"", toml);
        Assert.Contains("summary = \"Test Summary\"", toml);
        Assert.Contains("practice_mode = true", toml);
    }

    [Fact]
    public void FromToml_DeserializeString_Success()
    {
        // Arrange
        var toml = """"
            title = "Test Game"
            summary = "Test Summary"
            content = """
            Test Content
            This is a multi-line content.
            It should be preserved correctly.
            """
            hidden = false
            practice_mode = true
            accept_without_review = false
            team_member_count_limit = 5
            container_count_limit = 3
            start_time = 2025-10-01T00:00:00+00:00
            end_time = 2025-10-03T00:00:00+00:00
            """";

        const string multiLineContent = """
            Test Content
            This is a multi-line content.
            It should be preserved correctly.

            """;

        // Act
        var game = TransferHelper.FromToml<TransferGame>(toml);

        // Assert
        Assert.Equal("Test Game", game.Title);
        Assert.Equal("Test Summary", game.Summary);
        Assert.Equal(multiLineContent, game.Content);
        Assert.False(game.Hidden);
        Assert.True(game.PracticeMode);
        Assert.Equal(5, game.TeamMemberCountLimit);
    }

    [Fact]
    public void ComputeHash_StringContent_ReturnsCorrectFormat()
    {
        // Arrange
        var content = "test content";

        // Act
        var hash = TransferHelper.ComputeHash(content);

        // Assert
        Assert.StartsWith("sha256:", hash);
        Assert.Equal(71, hash.Length); // "sha256:" (7) + 64 hex chars
        Assert.Matches("^sha256:[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeHash_ByteContent_ReturnsCorrectFormat()
    {
        // Arrange
        var content = "test content"u8.ToArray();

        // Act
        var hash = TransferHelper.ComputeHash(content);

        // Assert
        Assert.StartsWith("sha256:", hash);
        Assert.Equal(71, hash.Length);
        Assert.Matches("^sha256:[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeHash_SameContent_ReturnsSameHash()
    {
        // Arrange
        var content1 = "identical content";
        var content2 = "identical content";

        // Act
        var hash1 = TransferHelper.ComputeHash(content1);
        var hash2 = TransferHelper.ComputeHash(content2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ReturnsDifferentHash()
    {
        // Arrange
        var content1 = "content 1";
        var content2 = "content 2";

        // Act
        var hash1 = TransferHelper.ComputeHash(content1);
        var hash2 = TransferHelper.ComputeHash(content2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetExporterVersion_ReturnsValidVersion()
    {
        // Act
        var version = TransferHelper.GetExporterVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.NotEqual("unknown", version);
    }

    [Fact]
    public void RoundTrip_SerializeAndDeserialize_Success()
    {
        const string multiLineContent = """
            # This is a test content
            It spans multiple lines.
            - Item 1
            - Item 2
            """;

        // Arrange
        var original = new TransferGame
        {
            Title = "Round Trip Test",
            Summary = "Testing serialization",
            Content = multiLineContent,
            Hidden = true,
            PracticeMode = false,
            AcceptWithoutReview = true,
            InviteCode = "ROUND",
            TeamMemberCountLimit = 10,
            ContainerCountLimit = 5,
            StartTime = new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 12, 5, 0, 0, 0, TimeSpan.Zero)
        };

        TransferGame? deserialized = null;
        // Act
        try
        {
            var toml = TransferHelper.ToToml(original);
            output.WriteLine("Serialized TOML:\n" + toml);

            deserialized = TransferHelper.FromToml<TransferGame>(toml);
        }
        catch (Exception ex)
        {
            output.WriteLine("Exception during round-trip:\n" + ex);
        }

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Title, deserialized.Title);
        Assert.Equal(original.Summary, deserialized.Summary);
        Assert.Equal(original.Content, deserialized.Content);
        Assert.Equal(original.Hidden, deserialized.Hidden);
        Assert.Equal(original.PracticeMode, deserialized.PracticeMode);
        Assert.Equal(original.AcceptWithoutReview, deserialized.AcceptWithoutReview);
        Assert.Equal(original.InviteCode, deserialized.InviteCode);
        Assert.Equal(original.TeamMemberCountLimit, deserialized.TeamMemberCountLimit);
        Assert.Equal(original.ContainerCountLimit, deserialized.ContainerCountLimit);
    }
}
