using System;
using GZCTF.Models.Data;
using GZCTF.Models.Transfer;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Transfer;

public class TransferChallengeTests
{
    [Fact]
    public void ToTransfer_StaticChallenge_Success()
    {
        // Arrange
        var challenge = new GameChallenge
        {
            Id = 1,
            Title = "Web Challenge",
            Content = "# Find the flag",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 1000,
            MinScoreRate = 0.25,
            Difficulty = 5.0,
            SubmissionLimit = 0,
            DeadlineUtc = DateTimeOffset.UtcNow.AddDays(7),
            FlagTemplate = null,
            DisableBloodBonus = false,
            EnableTrafficCapture = false,
            Hints = ["Hint 1", "Hint 2"],
            Flags = [new() { Flag = "flag{test_flag}" }]
        };

        // Act
        var transfer = challenge.ToTransfer();

        // Assert
        Assert.Equal(1, transfer.Id);
        Assert.Equal("Web Challenge", transfer.Title);
        Assert.Equal("# Find the flag", transfer.Content);
        Assert.Equal(ChallengeCategory.Web, transfer.Category);
        Assert.Equal(ChallengeType.StaticAttachment, transfer.Type);
        Assert.True(transfer.Enabled);
        Assert.Equal(1000, transfer.Scoring.Original);
        Assert.Equal(0.25, transfer.Scoring.MinRate);
        Assert.Equal(5.0, transfer.Scoring.Difficulty);
        Assert.Equal(0, transfer.Limits.Submission);
        Assert.Null(transfer.Flags.Template);
        Assert.False(transfer.Flags.DisableBloodBonus);
        Assert.Equal(2, transfer.Hints!.Count);
        Assert.NotNull(transfer.Flags.Static);
        Assert.Single(transfer.Flags.Static);
        Assert.Equal("flag{test_flag}", transfer.Flags.Static[0].Value);
        Assert.Null(transfer.Container);
    }

    [Fact]
    public void ToTransfer_ContainerChallenge_Success()
    {
        // Arrange
        var challenge = new GameChallenge
        {
            Id = 2,
            Title = "Pwn Challenge",
            Content = "Exploit the binary",
            Category = ChallengeCategory.Pwn,
            Type = ChallengeType.DynamicContainer,
            IsEnabled = true,
            OriginalScore = 500,
            MinScoreRate = 0.5,
            Difficulty = 3.0,
            FlagTemplate = "flag{[TEAM_HASH]_pwn}",
            ContainerImage = "ctf/pwn:latest",
            MemoryLimit = 128,
            CPUCount = 2,
            StorageLimit = 512,
            ContainerExposePort = 9999,
            FileName = "exploit",
            Flags = []
        };

        // Act
        var transfer = challenge.ToTransfer();

        // Assert
        Assert.Equal(ChallengeCategory.Pwn, transfer.Category);
        Assert.Equal(ChallengeType.DynamicContainer, transfer.Type);
        Assert.Equal("flag{[TEAM_HASH]_pwn}", transfer.Flags.Template);

        Assert.NotNull(transfer.Container);
        Assert.Equal("ctf/pwn:latest", transfer.Container.Image);
        Assert.Equal(128, transfer.Container.MemoryLimit);
        Assert.Equal(2, transfer.Container.CpuCount);
        Assert.Equal(512, transfer.Container.StorageLimit);
        Assert.Equal(9999, transfer.Container.ExposePort);
        Assert.Equal("exploit", transfer.Container.FileName);
    }

    static readonly TransferChallenge TransferChallenge = new()
    {
        Id = 3,
        Title = "Crypto Challenge",
        Content = "Decrypt this",
        Category = ChallengeCategory.Crypto,
        Type = ChallengeType.StaticAttachment,
        Enabled = true,
        Scoring = new ScoringSection
        {
            Original = 800,
            MinRate = 0.3,
            Difficulty = 4.0
        },
        Limits = new LimitsSection
        {
            Submission = 10,
            Deadline = DateTimeOffset.UtcNow.AddDays(5)
        },
        Flags = new FlagsSection
        {
            Template = null,
            DisableBloodBonus = true,
            EnableTrafficCapture = false,
            Static = [new() { Value = "flag{crypto_master}" }]
        },
        Hints = ["RSA", "Small e"]
    };

    [Fact]
    public void ToChallenge_ConvertTransfer_Success()
    {
        // Act
        var challenge = TransferChallenge.ToChallenge();

        // Assert
        Assert.Equal("Crypto Challenge", challenge.Title);
        Assert.Equal("Decrypt this", challenge.Content);
        Assert.Equal(ChallengeCategory.Crypto, challenge.Category);
        Assert.Equal(ChallengeType.StaticAttachment, challenge.Type);
        Assert.True(challenge.IsEnabled);
        Assert.Equal(800, challenge.OriginalScore);
        Assert.Equal(0.3, challenge.MinScoreRate);
        Assert.Equal(4.0, challenge.Difficulty);
        Assert.Equal(10, challenge.SubmissionLimit);
        Assert.True(challenge.DisableBloodBonus);
        Assert.Equal(2, challenge.Hints!.Count);
    }

    [Fact]
    public void ToJson_SerializeObject_Success()
    {
        // Arrange
        var json = TransferHelper.ToJson(TransferChallenge);

        // Act
        var deserialized = TransferHelper.FromJson<TransferChallenge>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(TransferChallenge.Title, deserialized.Title);
        Assert.Equal(TransferChallenge.Category, deserialized.Category);
        Assert.Equal(TransferChallenge.Scoring.Original, deserialized.Scoring.Original);
        Assert.Equal(TransferChallenge.Flags.Static![0].Value, deserialized.Flags.Static?[0].Value);
        Assert.Equal(TransferChallenge.Flags.Static![0].Attachment, deserialized.Flags.Static?[0].Attachment);
        Assert.Equal(TransferChallenge.Flags.Template, deserialized.Flags.Template);
        Assert.Equal(TransferChallenge.Limits.Submission, deserialized.Limits.Submission);
    }

    [Fact]
    public void ToFlagContext_ConvertStaticFlag_Success()
    {
        // Arrange
        var staticFlag = new StaticFlagSection
        {
            Value = "flag{test_value}"
        };

        // Act
        var flagContext = staticFlag.ToFlagContext();

        // Assert
        Assert.Equal("flag{test_value}", flagContext.Flag);
        Assert.False(flagContext.IsOccupied);
    }
}
