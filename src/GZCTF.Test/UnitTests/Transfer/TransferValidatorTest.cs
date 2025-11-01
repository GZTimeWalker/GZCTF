using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Transfer;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests;

public class TransferValidatorTest
{
    [Fact]
    public void ValidateGameData_ValidGame_ShouldPass()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            TeamMemberCountLimit = 5
        };

        // Should not throw
        TransferValidator.ValidateRecursive(game, "Game");
    }

    [Fact]
    public void ValidateGameData_InvalidEndTime_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow.AddDays(7),
            EndTime = DateTimeOffset.UtcNow, // End before start
            ContainerCountLimit = 3
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("End time must be after start time", ex.Message);
    }

    [Fact]
    public void ValidateGameData_InvalidBloodBonus_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            BloodBonus = new BloodBonusSection
            {
                First = 2000, // Exceeds max 1023
                Second = 100,
                Third = 50
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("First blood bonus must be 0-1023", ex.Message);
    }

    [Fact]
    public void ValidateGameData_InvalidPosterHash_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            PosterHash = "invalid-hash" // Not 64 hex chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Poster hash must be 64 hex characters", ex.Message);
    }

    [Fact]
    public void ValidateGameData_InvalidInviteCode_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            InviteCode = "test@code!" // Contains invalid chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Invite code contains invalid characters", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_ValidChallenge_ShouldPass()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Scoring = new ScoringSection
            {
                Original = 500,
                MinRate = 0.25,
                Difficulty = 5.0
            },
            Flags = new FlagsSection
            {
                Static = new List<StaticFlagSection>
                {
                    new() { Value = "flag{test}" }
                }
            }
        };

        // Should not throw
        TransferValidator.ValidateRecursive(challenge, "Challenge");
    }

    [Fact]
    public void ValidateChallenge_NoFlags_ShouldPass()
    {
        // Challenges without flags are allowed for import scenarios
        // since they are disabled by default and can be configured later
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Flags = new FlagsSection() // No template, no static flags
        };

        // Should not throw - incomplete data allowed for import
        TransferValidator.ValidateRecursive(challenge, "Challenge");
    }

    [Fact]
    public void ValidateChallenge_InvalidScoring_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Scoring = new ScoringSection
            {
                Original = -100, // Invalid negative score
                MinRate = 0.25,
                Difficulty = 5.0
            },
            Flags = new FlagsSection
            {
                Static = new List<StaticFlagSection> { new() { Value = "flag{test}" } }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Original score must be positive", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_InvalidMinRate_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Scoring = new ScoringSection
            {
                Original = 500,
                MinRate = 1.5, // Exceeds max 1.0
                Difficulty = 5.0
            },
            Flags = new FlagsSection
            {
                Static = new List<StaticFlagSection> { new() { Value = "flag{test}" } }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Minimum score rate must be between 0 and 1", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_ValidLocalAttachment_ShouldPass()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            Hash = "a".PadRight(64, '0'), // 64 hex chars
            FileName = "test.zip",
            FileSize = 1024
        };

        // Should not throw
        TransferValidator.ValidateRecursive(attachment, "Attachment");
    }

    [Fact]
    public void ValidateAttachment_LocalMissingHash_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            FileName = "test.zip",
            FileSize = 1024
            // Missing Hash
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Local attachment must have a file hash", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_RemoteMissingUrl_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Remote,
            FileName = "test.zip"
            // Missing RemoteUrl
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Remote attachment must have a URL", ex.Message);
    }

    [Fact]
    public void ValidateContainer_ValidContainer_ShouldPass()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            MemoryLimit = 128,
            CpuCount = 2,
            StorageLimit = 512,
            ExposePort = 8080
        };

        // Should not throw
        TransferValidator.ValidateRecursive(container, "Container");
    }

    [Fact]
    public void ValidateContainer_InvalidPort_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            ExposePort = 99999 // Exceeds max 65535
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Exposed port must be 1-65535", ex.Message);
    }

    [Fact]
    public void ValidateDivision_ValidDivision_ShouldPass()
    {
        var division = new TransferDivision
        {
            Name = "Test Division",
            InviteCode = "test-code"
        };

        // Should not throw
        TransferValidator.ValidateRecursive(division, "Division");
    }

    [Fact]
    public void ValidateDivision_TooLongName_ShouldThrow()
    {
        var division = new TransferDivision
        {
            Name = new string('a', 50) // Exceeds max 31 chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(division, "Division"));
        Assert.Contains("Division name is too long", ex.Message);
    }

    // ============= Additional comprehensive tests for missing validations =============

    #region TransferGame Validation Tests

    [Fact]
    public void ValidateGame_EmptyTitle_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Game title is required", ex.Message);
    }

    [Fact]
    public void ValidateGame_ZeroContainerCountLimit_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 0 // Must be positive
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Container count limit must be positive", ex.Message);
    }

    [Fact]
    public void ValidateGame_NegativeTeamMemberCountLimit_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            TeamMemberCountLimit = -1 // Must be non-negative
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Team member count limit must be non-negative", ex.Message);
    }

    [Fact]
    public void ValidateGame_WriteupDeadlineBeforeEndTime_ShouldThrow()
    {
        var endTime = DateTimeOffset.UtcNow.AddDays(7);
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = endTime,
            ContainerCountLimit = 3,
            Writeup = new WriteupSection
            {
                Required = true,
                Deadline = endTime.AddHours(-1) // Before end time
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Writeup deadline must be after game end time", ex.Message);
    }

    [Fact]
    public void ValidateGame_BloodBonusSecondTooHigh_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            BloodBonus = new BloodBonusSection
            {
                First = 500,
                Second = 1024, // Exceeds max 1023
                Third = 100
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Second blood bonus must be 0-1023", ex.Message);
    }

    [Fact]
    public void ValidateGame_BloodBonusThirdNegative_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            BloodBonus = new BloodBonusSection
            {
                First = 500,
                Second = 300,
                Third = -1 // Negative value
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Third blood bonus must be 0-1023", ex.Message);
    }

    [Fact]
    public void ValidateGame_InviteCodeTooLong_ShouldThrow()
    {
        var game = new TransferGame
        {
            Title = "Test Game",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(7),
            ContainerCountLimit = 3,
            InviteCode = new string('a', 40) // Exceeds 32 chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(game, "Game"));
        Assert.Contains("Invite code is too long", ex.Message);
    }

    #endregion

    #region TransferChallenge Validation Tests

    [Fact]
    public void ValidateChallenge_EmptyTitle_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Challenge title is required", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_NegativeDifficulty_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Scoring = new ScoringSection
            {
                Original = 500,
                MinRate = 0.25,
                Difficulty = -1.0 // Negative
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Difficulty coefficient must be non-negative", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_NegativeSubmissionLimit_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Limits = new LimitsSection
            {
                Submission = -1 // Negative
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Submission limit must be non-negative", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_ContainerTypeWithoutContainer_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.DynamicContainer,
            Container = null // Missing container config
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Container challenges must have container configuration", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_StaticFlagEmptyValue_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Flags = new FlagsSection
            {
                Static = new List<StaticFlagSection>
                {
                    new() { Value = "" } // Empty flag value
                }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Flag value is required", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_StaticFlagTooLong_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            Flags = new FlagsSection
            {
                Static = new List<StaticFlagSection>
                {
                    new() { Value = new string('a', 150) } // Exceeds 127 chars
                }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Flag value is too long", ex.Message);
    }

    [Fact]
    public void ValidateChallenge_TemplateTooLong_ShouldThrow()
    {
        var challenge = new TransferChallenge
        {
            Title = "Test Challenge",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.DynamicAttachment,
            Flags = new FlagsSection
            {
                Template = new string('a', 130) // Exceeds 120 chars
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(challenge, "Challenge"));
        Assert.Contains("Flag template is too long", ex.Message);
    }

    #endregion

    #region AttachmentSection Validation Tests

    [Fact]
    public void ValidateAttachment_LocalMissingFileName_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            Hash = "a".PadRight(64, '0'),
            FileSize = 1024
            // Missing FileName
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Local attachment must have a file name", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_LocalZeroFileSize_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            Hash = "a".PadRight(64, '0'),
            FileName = "test.zip",
            FileSize = 0 // Invalid
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Local attachment must have a valid file size", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_LocalNegativeFileSize_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            Hash = "a".PadRight(64, '0'),
            FileName = "test.zip",
            FileSize = -100 // Negative
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("File size must be non-negative", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_InvalidHashFormat_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Local,
            Hash = "invalid-hash-xyz", // Not hex
            FileName = "test.zip",
            FileSize = 1024
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("File hash must be 64 hex characters", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_RemoteMissingFileName_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Remote,
            RemoteUrl = "https://example.com/file.zip"
            // Missing FileName
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Remote attachment must have a file name", ex.Message);
    }

    [Fact]
    public void ValidateAttachment_InvalidRemoteUrl_ShouldThrow()
    {
        var attachment = new AttachmentSection
        {
            Type = FileType.Remote,
            RemoteUrl = "not-a-valid-url",
            FileName = "test.zip"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(attachment, "Attachment"));
        Assert.Contains("Remote URL is invalid", ex.Message);
    }

    #endregion

    #region ContainerSection Validation Tests

    [Fact]
    public void ValidateContainer_EmptyImage_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = ""
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Container image is required", ex.Message);
    }

    [Fact]
    public void ValidateContainer_ZeroMemoryLimit_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            MemoryLimit = 0
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Memory limit must be positive", ex.Message);
    }

    [Fact]
    public void ValidateContainer_NegativeCpuCount_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            CpuCount = -1
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("CPU count must be positive", ex.Message);
    }

    [Fact]
    public void ValidateContainer_ZeroStorageLimit_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            StorageLimit = 0
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Storage limit must be positive", ex.Message);
    }

    [Fact]
    public void ValidateContainer_PortZero_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            ExposePort = 0
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Exposed port must be 1-65535", ex.Message);
    }

    [Fact]
    public void ValidateContainer_PortTooHigh_ShouldThrow()
    {
        var container = new ContainerSection
        {
            Image = "nginx:latest",
            ExposePort = 70000
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(container, "Container"));
        Assert.Contains("Exposed port must be 1-65535", ex.Message);
    }

    #endregion

    #region TransferDivision Validation Tests

    [Fact]
    public void ValidateDivision_EmptyName_ShouldThrow()
    {
        var division = new TransferDivision
        {
            Name = ""
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(division, "Division"));
        Assert.Contains("Division name is required", ex.Message);
    }

    [Fact]
    public void ValidateDivision_InviteCodeTooLong_ShouldThrow()
    {
        var division = new TransferDivision
        {
            Name = "Test Division",
            InviteCode = new string('a', 40) // Exceeds 32 chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(division, "Division"));
        Assert.Contains("Invite code is too long", ex.Message);
    }

    [Fact]
    public void ValidateDivision_InviteCodeInvalidChars_ShouldThrow()
    {
        var division = new TransferDivision
        {
            Name = "Test Division",
            InviteCode = "test@code!" // Invalid chars
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            TransferValidator.ValidateRecursive(division, "Division"));
        Assert.Contains("Invite code contains invalid characters", ex.Message);
    }

    #endregion
}
