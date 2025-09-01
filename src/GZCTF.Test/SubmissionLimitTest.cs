using GZCTF.Models.Data;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test;

public class SubmissionLimitTest
{
    [Fact]
    public void Challenge_SubmissionLimit_ShouldBeNullableForBackwardCompatibility()
    {
        // Arrange & Act
        var challenge = new Challenge
        {
            Title = "Test Challenge",
            Content = "Test content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment
        };

        // Assert
        Assert.Null(challenge.SubmissionLimit);
    }

    [Fact]
    public void Challenge_SubmissionLimit_ShouldAcceptPositiveValues()
    {
        // Arrange & Act
        var challenge = new Challenge
        {
            Title = "Test Challenge",
            Content = "Test content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            SubmissionLimit = 10
        };

        // Assert
        Assert.Equal(10, challenge.SubmissionLimit);
    }

    [Fact]
    public void GameChallenge_Update_ShouldHandleSubmissionLimit()
    {
        // Arrange
        var gameChallenge = new GameChallenge
        {
            Title = "Test Challenge",
            Content = "Test content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            SubmissionLimit = 5
        };

        var updateModel = new Models.Request.Edit.ChallengeUpdateModel
        {
            SubmissionLimit = 15
        };

        // Act
        gameChallenge.Update(updateModel);

        // Assert
        Assert.Equal(15, gameChallenge.SubmissionLimit);
    }

    [Fact]
    public void GameChallenge_Update_ShouldNotChangeSubmissionLimitWhenNull()
    {
        // Arrange
        var gameChallenge = new GameChallenge
        {
            Title = "Test Challenge",
            Content = "Test content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            SubmissionLimit = 5
        };

        var updateModel = new Models.Request.Edit.ChallengeUpdateModel
        {
            SubmissionLimit = null
        };

        // Act
        gameChallenge.Update(updateModel);

        // Assert
        Assert.Equal(5, gameChallenge.SubmissionLimit);
    }

    [Fact]
    public void ChallengeUpdateModel_SubmissionLimit_ShouldValidateRange()
    {
        // Arrange
        var updateModel = new Models.Request.Edit.ChallengeUpdateModel();

        // Test valid range
        updateModel.SubmissionLimit = 1;
        Assert.Equal(1, updateModel.SubmissionLimit);

        updateModel.SubmissionLimit = 500;
        Assert.Equal(500, updateModel.SubmissionLimit);

        updateModel.SubmissionLimit = 1000;
        Assert.Equal(1000, updateModel.SubmissionLimit);

        // Test null value (should be allowed)
        updateModel.SubmissionLimit = null;
        Assert.Null(updateModel.SubmissionLimit);
    }

    [Fact]
    public void Instance_SubmissionCount_ShouldDefaultToZero()
    {
        // Arrange & Act
        var instance = new GameInstance();

        // Assert
        Assert.Equal(0, instance.SubmissionCount);
    }

    [Fact]
    public void Instance_SubmissionCount_ShouldBeIncremented()
    {
        // Arrange
        var instance = new GameInstance();

        // Act
        instance.SubmissionCount++;

        // Assert
        Assert.Equal(1, instance.SubmissionCount);
    }
}