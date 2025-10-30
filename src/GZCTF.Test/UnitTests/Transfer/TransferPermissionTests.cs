using System.Collections.Generic;
using GZCTF.Models.Transfer;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Transfer;

public class TransferPermissionTests
{
    [Fact]
    public void PermissionsToStrings_SinglePermission_Success()
    {
        // Arrange
        var permission = GamePermission.ViewChallenge;

        // Act
        var strings = TransferExtensions.PermissionsToStrings(permission);

        // Assert
        Assert.Single(strings);
        Assert.Contains("ViewChallenge", strings);
    }

    [Fact]
    public void PermissionsToStrings_MultiplePermissions_Success()
    {
        // Arrange
        var permissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags | GamePermission.GetScore;

        // Act
        var strings = TransferExtensions.PermissionsToStrings(permissions);

        // Assert
        Assert.Equal(3, strings.Count);
        Assert.Contains("ViewChallenge", strings);
        Assert.Contains("SubmitFlags", strings);
        Assert.Contains("GetScore", strings);
    }

    [Fact]
    public void PermissionsToStrings_AllPermission_ReturnsAll()
    {
        // Arrange
        var permission = GamePermission.All;

        // Act
        var strings = TransferExtensions.PermissionsToStrings(permission);

        // Assert
        Assert.Single(strings);
        Assert.Equal("All", strings[0]);
    }

    [Fact]
    public void PermissionsToStrings_NoPermission_ReturnsNone()
    {
        // Arrange
        var permission = (GamePermission)0;

        // Act
        var strings = TransferExtensions.PermissionsToStrings(permission);

        // Assert
        Assert.Single(strings);
        Assert.Equal("None", strings[0]);
    }

    [Fact]
    public void StringsToPermissions_SinglePermission_Success()
    {
        // Arrange
        var strings = new List<string> { "ViewChallenge" };

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal(GamePermission.ViewChallenge, permission);
    }

    [Fact]
    public void StringsToPermissions_MultiplePermissions_Success()
    {
        // Arrange
        var strings = new List<string> { "ViewChallenge", "SubmitFlags", "GetScore" };

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        var expected = GamePermission.ViewChallenge | GamePermission.SubmitFlags | GamePermission.GetScore;
        Assert.Equal(expected, permission);
    }

    [Fact]
    public void StringsToPermissions_All_ReturnsAll()
    {
        // Arrange
        var strings = new List<string> { "All" };

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal(GamePermission.All, permission);
    }

    [Fact]
    public void StringsToPermissions_None_ReturnsNone()
    {
        // Arrange
        var strings = new List<string> { "None" };

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal((GamePermission)0, permission);
    }

    [Fact]
    public void StringsToPermissions_Null_ReturnsNone()
    {
        // Arrange
        List<string>? strings = null;

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal((GamePermission)0, permission);
    }

    [Fact]
    public void StringsToPermissions_Empty_ReturnsNone()
    {
        // Arrange
        var strings = new List<string>();

        // Act
        var permission = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal((GamePermission)0, permission);
    }

    [Fact]
    public void PermissionsRoundTrip_PreservesValue()
    {
        // Arrange
        var original = GamePermission.ViewChallenge | GamePermission.SubmitFlags | GamePermission.GetScore;

        // Act
        var strings = TransferExtensions.PermissionsToStrings(original);
        var restored = TransferExtensions.StringsToPermissions(strings);

        // Assert
        Assert.Equal(original, restored);
    }
}
