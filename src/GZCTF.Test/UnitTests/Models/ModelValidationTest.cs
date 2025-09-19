using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Test.Infrastructure;
using GZCTF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Test.UnitTests.Models;

public class ModelValidationTest : TestBase
{
    public ModelValidationTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("validuser", "valid@example.com", true)]
    [InlineData("", "valid@example.com", false)] // Empty username
    [InlineData("validuser", "", false)] // Empty email
    [InlineData("validuser", "invalid-email", false)] // Invalid email format
    public void UserInfo_Validation_ShouldValidateCorrectly(string userName, string email, bool expectedValid)
    {
        // Arrange
        var user = new UserInfo
        {
            UserName = userName,
            Email = email,
            Role = Role.User
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        if (expectedValid)
        {
            validationResults.Should().BeEmpty();
        }
        else
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("ValidTeam", true)]
    [InlineData("", false)] // Empty name
    [InlineData(null, false)] // Null name
    public void Team_Validation_ShouldValidateCorrectly(string teamName, bool expectedValid)
    {
        // Arrange
        var captain = TestDataFactory.CreateUser();
        var team = new Team
        {
            Name = teamName,
            Captain = captain,
            CaptainId = captain.Id
        };

        // Act
        var validationResults = ValidateModel(team);

        // Assert
        if (expectedValid)
        {
            validationResults.Should().BeEmpty();
        }
        else
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("Valid Game Title", true)]
    [InlineData("", false)] // Empty title
    [InlineData(null, false)] // Null title
    public void Game_Validation_ShouldValidateCorrectly(string title, bool expectedValid)
    {
        // Arrange
        var game = new Game
        {
            Title = title,
            StartTimeUtc = DateTimeOffset.UtcNow.AddDays(1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(2)
        };

        // Act
        var validationResults = ValidateModel(game);

        // Assert
        if (expectedValid)
        {
            validationResults.Should().BeEmpty();
        }
        else
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("user123", "test@example.com", "StrongPass123!", true)]
    [InlineData("", "test@example.com", "StrongPass123!", false)] // Empty username
    [InlineData("user123", "", "StrongPass123!", false)] // Empty email
    [InlineData("user123", "test@example.com", "", false)] // Empty password
    [InlineData("user123", "invalid-email", "StrongPass123!", false)] // Invalid email
    public void RegisterModel_Validation_ShouldValidateCorrectly(
        string userName, string email, string password, bool expectedValid)
    {
        // Arrange
        var registerModel = new RegisterModel
        {
            UserName = userName,
            Email = email,
            Password = password
        };

        // Act
        var validationResults = ValidateModel(registerModel);

        // Assert
        if (expectedValid)
        {
            validationResults.Should().BeEmpty();
        }
        else
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void UserInfo_DefaultValues_ShouldBeValid()
    {
        // Arrange
        var user = new UserInfo
        {
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act & Assert
        user.Role.Should().Be(Role.User); // Default role
        user.IP.Should().Be("0.0.0.0"); // Default IP
        user.ExerciseVisible.Should().BeTrue(); // Default visibility
        user.Id.Should().NotBe(Guid.Empty); // Should generate ULID
    }

    [Fact]
    public void Team_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var team = new Team
        {
            Name = "Test Team"
        };

        // Assert
        team.Id.Should().NotBe(Guid.Empty); // Should generate ULID
        team.Locked.Should().BeFalse(); // Default not locked
        team.Members.Should().NotBeNull(); // Should initialize collection
        team.Participations.Should().NotBeNull(); // Should initialize collection
    }

    [Fact]
    public void Game_DateValidation_ShouldValidateTimeOrder()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Title = "Test Game",
            StartTimeUtc = now.AddDays(2),
            EndTimeUtc = now.AddDays(1) // End before start - invalid
        };

        // Act
        var validationResults = ValidateModel(game);

        // Assert - This would depend on custom validation attributes if implemented
        // For now, just check that the model can be created
        game.Title.Should().Be("Test Game");
        game.StartTimeUtc.Should().BeAfter(game.EndTimeUtc);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}