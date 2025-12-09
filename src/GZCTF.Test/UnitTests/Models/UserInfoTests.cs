using System.Collections.Generic;
using System.Text.Json;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

public class UserInfoTests
{
    [Fact]
    public void UpdateUserInfo_ProfileUpdateModel_UpdatesMetadata()
    {
        var user = new UserInfo();
        var metadata = new SortedDictionary<string, JsonDocument?>
        {
            ["key1"] = JsonSerializer.Deserialize<JsonDocument>("\"value1\"")
        };
        var model = new ProfileUpdateModel { Metadata = metadata };

        user.UpdateUserInfo(model);

        Assert.NotNull(user.Metadata);
        Assert.Equal("value1", user.Metadata["key1"]?.RootElement.GetString());
    }

    [Fact]
    public void UpdateUserInfo_AdminUserInfoModel_UpdatesMetadata()
    {
        var user = new UserInfo();
        var metadata = new SortedDictionary<string, JsonDocument?>
        {
            ["key2"] = JsonSerializer.Deserialize<JsonDocument>("\"value2\"")
        };
        var model = new AdminUserInfoModel { Metadata = metadata };

        user.UpdateUserInfo(model);

        Assert.NotNull(user.Metadata);
        Assert.Equal("value2", user.Metadata["key2"]?.RootElement.GetString());
    }

    [Fact]
    public void UpdateUserInfo_UserCreateModel_UpdatesMetadata()
    {
        var user = new UserInfo();
        var metadata = new SortedDictionary<string, JsonDocument?>
        {
            ["key3"] = JsonSerializer.Deserialize<JsonDocument>("\"value3\"")
        };
        var model = new UserCreateModel
        {
            UserName = "test", Password = "password", Email = "test@example.com", Metadata = metadata
        };

        user.UpdateUserInfo(model);

        Assert.NotNull(user.Metadata);
        Assert.Equal("value3", user.Metadata["key3"]?.RootElement.GetString());
    }

    [Fact]
    public void UpdateUserInfo_ProfileUpdateModel_NullMetadata_DoesNotUpdate()
    {
        var initialMetadata = new SortedDictionary<string, JsonDocument?>
        {
            ["key1"] = JsonSerializer.Deserialize<JsonDocument>("\"initial\"")
        };
        var user = new UserInfo { Metadata = initialMetadata };
        var model = new ProfileUpdateModel { Metadata = null };

        user.UpdateUserInfo(model);

        Assert.NotNull(user.Metadata);
        Assert.Equal("initial", user.Metadata["key1"]?.RootElement.GetString());
    }
}
