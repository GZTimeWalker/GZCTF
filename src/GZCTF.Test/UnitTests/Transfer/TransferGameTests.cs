using System;
using GZCTF.Models.Data;
using GZCTF.Models.Transfer;
using Xunit;

namespace GZCTF.Test.UnitTests.Transfer;

public class TransferGameTests
{
    [Fact]
    public void ToTransfer_ConvertGame_Success()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Title = "Test CTF 2025",
            Summary = "A test competition",
            Content = "# Test Content",
            Hidden = false,
            PracticeMode = true,
            AcceptWithoutReview = true,
            InviteCode = "TEST2025",
            TeamMemberCountLimit = 5,
            ContainerCountLimit = 3,
            StartTimeUtc = new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.FromHours(8)),
            EndTimeUtc = new DateTimeOffset(2025, 10, 3, 23, 59, 59, TimeSpan.FromHours(8)),
            WriteupRequired = true,
            WriteupDeadline = new DateTimeOffset(2025, 10, 10, 23, 59, 59, TimeSpan.FromHours(8)),
            WriteupNote = "Please submit writeup",
            BloodBonusValue = (50L << 20) + (30L << 10) + 10,
            PosterHash = "abc123"
        };

        // Act
        var transfer = game.ToTransfer();

        // Assert
        Assert.Equal("Test CTF 2025", transfer.Title);
        Assert.Equal("A test competition", transfer.Summary);
        Assert.Equal("# Test Content", transfer.Content);
        Assert.False(transfer.Hidden);
        Assert.True(transfer.PracticeMode);
        Assert.True(transfer.AcceptWithoutReview);
        Assert.Equal("TEST2025", transfer.InviteCode);
        Assert.Equal(5, transfer.TeamMemberCountLimit);
        Assert.Equal(3, transfer.ContainerCountLimit);

        // Writeup
        Assert.NotNull(transfer.Writeup);
        Assert.True(transfer.Writeup.Required);
        Assert.Equal("Please submit writeup", transfer.Writeup.Note);

        // Blood bonus
        Assert.NotNull(transfer.BloodBonus);
        Assert.Equal(50, transfer.BloodBonus.First);
        Assert.Equal(30, transfer.BloodBonus.Second);
        Assert.Equal(10, transfer.BloodBonus.Third);

        // Assets
        Assert.Equal("abc123", transfer.PosterHash);
    }

    [Fact]
    public void ToGame_ConvertTransfer_Success()
    {
        // Arrange
        var transfer = new TransferGame
        {
            Title = "Import Test",
            Summary = "Test summary",
            Content = "Test content",
            Hidden = false,
            PracticeMode = true,
            AcceptWithoutReview = false,
            InviteCode = "IMPORT",
            TeamMemberCountLimit = 4,
            ContainerCountLimit = 2,
            StartTime = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 11, 3, 0, 0, 0, TimeSpan.Zero),
            Writeup = new WriteupSection
            {
                Required = false,
                Deadline = new DateTimeOffset(2025, 11, 10, 0, 0, 0, TimeSpan.Zero),
                Note = "Optional writeup"
            },
            BloodBonus = new BloodBonusSection
            {
                First = 100,
                Second = 50,
                Third = 20
            },
            PosterHash = "def456"
        };

        // Act
        var game = transfer.ToGame();

        // Assert
        Assert.Equal("Import Test", game.Title);
        Assert.Equal("Test summary", game.Summary);
        Assert.Equal("Test content", game.Content);
        Assert.False(game.Hidden);
        Assert.True(game.PracticeMode);
        Assert.False(game.AcceptWithoutReview);
        Assert.Equal("IMPORT", game.InviteCode);
        Assert.Equal(4, game.TeamMemberCountLimit);
        Assert.Equal(2, game.ContainerCountLimit);

        // Writeup
        Assert.False(game.WriteupRequired);
        Assert.Equal("Optional writeup", game.WriteupNote);

        // Blood bonus
        var bloodBonus = game.BloodBonus;
        Assert.Equal(100, (int)bloodBonus.FirstBlood);
        Assert.Equal(50, (int)bloodBonus.SecondBlood);
        Assert.Equal(20, (int)bloodBonus.ThirdBlood);

        // Poster
        Assert.Equal("def456", game.PosterHash);

        // Key pair should be generated
        Assert.NotNull(game.PublicKey);
        Assert.NotNull(game.PrivateKey);
    }

    [Fact]
    public void ToTransfer_NoOptionalFields_Success()
    {
        // Arrange
        var game = new Game
        {
            Title = "Minimal Game",
            Summary = "Summary",
            Content = "Content",
            StartTimeUtc = DateTimeOffset.UtcNow,
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            BloodBonusValue = 0 // Explicitly set no bonus
        };

        // Act
        var transfer = game.ToTransfer();

        // Assert
        Assert.Equal("Minimal Game", transfer.Title);
        Assert.Null(transfer.InviteCode);
        Assert.Null(transfer.Writeup);
        Assert.Null(transfer.BloodBonus); // Should be null when BloodBonusValue is 0
        Assert.Null(transfer.PosterHash);
    }
}
