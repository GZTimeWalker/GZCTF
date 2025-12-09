using System.Text.Json;
using GZCTF.Extensions;
using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.Extensions.Localization;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

public class UserMetadataFieldValidationTests
{
    private readonly IStringLocalizer<Program> _localizer = Server.StaticLocalizer;

    [Fact]
    public void Validate_RequiredField_NullValue_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Required = true, Type = UserMetadataFieldType.Text };
        var value = JsonSerializer.Deserialize<JsonDocument>("null");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Field is required", error);
    }

    [Fact]
    public void Validate_RequiredField_EmptyString_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Required = true, Type = UserMetadataFieldType.Text };
        var value = JsonSerializer.Deserialize<JsonDocument>("\"\"");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Field is required", error);
    }

    [Fact]
    public void Validate_TextField_MaxLength_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Text, MaxLength = 5 };
        var value = JsonSerializer.Deserialize<JsonDocument>("\"123456\"");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Value exceeds maximum length of 5", error);
    }

    [Fact]
    public void Validate_TextField_Pattern_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Text, Pattern = "^[0-9]+$" };
        var value = JsonSerializer.Deserialize<JsonDocument>("\"abc\"");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Value does not match the required pattern", error);
    }

    [Fact]
    public void Validate_NumberField_MinValue_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Number, MinValue = 10 };
        var value = JsonSerializer.Deserialize<JsonDocument>("5");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Value must be at least 10", error);
    }

    [Fact]
    public void Validate_NumberField_MaxValue_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Number, MaxValue = 10 };
        var value = JsonSerializer.Deserialize<JsonDocument>("15");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Value must be at most 10", error);
    }

    [Fact]
    public void Validate_SelectField_InvalidOption_ReturnsFalse()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Select, Options = ["A", "B"] };
        var value = JsonSerializer.Deserialize<JsonDocument>("\"C\"");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Invalid option selected", error);
    }

    [Fact]
    public void Validate_MultiSelectField_InvalidOption_ReturnsFalse()
    {
        var field = new UserMetadataField
        {
            Key = "test",
            Type = UserMetadataFieldType.MultiSelect,
            Options = ["A", "B"]
        };
        var value = JsonSerializer.Deserialize<JsonDocument>("[\"A\", \"C\"]");

        var result = field.Validate(value, _localizer, out var error);

        Assert.False(result);
        Assert.Equal("Invalid option selected: C", error);
    }

    [Fact]
    public void Validate_ValidInput_ReturnsTrue()
    {
        var field = new UserMetadataField { Key = "test", Type = UserMetadataFieldType.Text, Required = true };
        var value = JsonSerializer.Deserialize<JsonDocument>("\"valid\"");

        var result = field.Validate(value, _localizer, out var error);

        Assert.True(result);
        Assert.Null(error);
    }
}
