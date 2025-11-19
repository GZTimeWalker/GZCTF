using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Tests for user metadata field configuration and profile management
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class UserMetadataTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task Admin_GetUserMetadataFields_ReturnsFields()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var client = factory.CreateAuthenticatedClient(admin);

        // Act
        var response = await client.GetAsync("/api/Admin/UserMetadata");

        // Assert
        response.EnsureSuccessStatusCode();
        var fields = await response.Content.ReadFromJsonAsync<List<UserMetadataField>>();
        Assert.NotNull(fields);
        // Database may or may not be empty depending on test execution order
        output.WriteLine($"Retrieved {fields.Count} metadata fields");
    }

    [Fact]
    public async Task Admin_CreateUserMetadataFields_Succeeds()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var client = factory.CreateAuthenticatedClient(admin);

        var fields = new List<UserMetadataField>
        {
            new()
            {
                Key = "department",
                DisplayName = "Department",
                Type = UserMetadataFieldType.Select,
                Required = true,
                Visible = true,
                Options = new List<string> { "Engineering", "Marketing", "Sales" }
            },
            new()
            {
                Key = "studentId",
                DisplayName = "Student ID",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true,
                MaxLength = 20
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/Admin/UserMetadata", fields);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify fields were created
        var getResponse = await client.GetAsync("/api/Admin/UserMetadata");
        getResponse.EnsureSuccessStatusCode();
        var retrievedFields = await getResponse.Content.ReadFromJsonAsync<List<UserMetadataField>>();
        Assert.NotNull(retrievedFields);
        Assert.Equal(2, retrievedFields.Count);
        Assert.Contains(retrievedFields, f => f.Key == "department");
        Assert.Contains(retrievedFields, f => f.Key == "studentId");
    }

    [Fact]
    public async Task Admin_UpdateUserMetadataFields_Succeeds()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var client = factory.CreateAuthenticatedClient(admin);

        // Create initial fields
        var initialFields = new List<UserMetadataField>
        {
            new()
            {
                Key = "organization",
                DisplayName = "Organization",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            }
        };
        await client.PutAsJsonAsync("/api/Admin/UserMetadata", initialFields);

        // Update fields
        var updatedFields = new List<UserMetadataField>
        {
            new()
            {
                Key = "organization",
                DisplayName = "Organization Name",
                Type = UserMetadataFieldType.Text,
                Required = true,
                Visible = true,
                MaxLength = 100
            },
            new()
            {
                Key = "role",
                DisplayName = "Role",
                Type = UserMetadataFieldType.Select,
                Required = true,
                Visible = true,
                Options = new List<string> { "Developer", "Manager", "Analyst" }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/Admin/UserMetadata", updatedFields);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify update
        var getResponse = await client.GetAsync("/api/Admin/UserMetadata");
        var retrievedFields = await getResponse.Content.ReadFromJsonAsync<List<UserMetadataField>>();
        Assert.NotNull(retrievedFields);
        Assert.Equal(2, retrievedFields.Count);
        
        var orgField = retrievedFields.FirstOrDefault(f => f.Key == "organization");
        Assert.NotNull(orgField);
        Assert.Equal("Organization Name", orgField.DisplayName);
        Assert.True(orgField.Required);
        Assert.Equal(100, orgField.MaxLength);
    }

    [Fact]
    public async Task Admin_DeleteAllUserMetadataFields_Succeeds()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        using var client = factory.CreateAuthenticatedClient(admin);

        // Create initial fields
        var fields = new List<UserMetadataField>
        {
            new()
            {
                Key = "tempField",
                DisplayName = "Temporary Field",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            }
        };
        await client.PutAsJsonAsync("/api/Admin/UserMetadata", fields);

        // Act - delete by sending empty list
        var response = await client.PutAsJsonAsync("/api/Admin/UserMetadata", new List<UserMetadataField>());

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify deletion
        var getResponse = await client.GetAsync("/api/Admin/UserMetadata");
        var retrievedFields = await getResponse.Content.ReadFromJsonAsync<List<UserMetadataField>>();
        Assert.NotNull(retrievedFields);
        Assert.Empty(retrievedFields);
    }

    [Fact]
    public async Task User_GetMetadataFields_ReturnsConfiguredFields()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        var (user, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.User);

        // Create fields as admin
        using var adminClient = factory.CreateAuthenticatedClient(admin);
        var fields = new List<UserMetadataField>
        {
            new()
            {
                Key = "userField",
                DisplayName = "User Field",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/UserMetadata", fields);

        // Act - get as regular user
        using var userClient = factory.CreateAuthenticatedClient(user);
        var response = await userClient.GetAsync("/api/Account/MetadataFields");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedFields = await response.Content.ReadFromJsonAsync<List<UserMetadataField>>();
        Assert.NotNull(retrievedFields);
        Assert.Single(retrievedFields);
        Assert.Equal("userField", retrievedFields[0].Key);
    }

    [Fact]
    public async Task User_UpdateProfile_WithMetadata_Succeeds()
    {
        // Arrange
        var (admin, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.Admin);
        var (user, password) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.User);

        // Create metadata fields as admin
        using var adminClient = factory.CreateAuthenticatedClient(admin);
        var fields = new List<UserMetadataField>
        {
            new()
            {
                Key = "department",
                DisplayName = "Department",
                Type = UserMetadataFieldType.Select,
                Required = false,
                Visible = true,
                Options = new List<string> { "IT", "HR", "Finance" }
            },
            new()
            {
                Key = "employeeId",
                DisplayName = "Employee ID",
                Type = UserMetadataFieldType.Text,
                Required = false,
                Visible = true
            }
        };
        await adminClient.PutAsJsonAsync("/api/Admin/UserMetadata", fields);

        // Act - update profile with metadata
        using var userClient = factory.CreateAuthenticatedClient(user);
        var updateModel = new ProfileUpdateModel
        {
            Bio = "Test bio",
            Metadata = new Dictionary<string, string>
            {
                { "department", "IT" },
                { "employeeId", "EMP001" }
            }
        };

        var response = await userClient.PutAsJsonAsync("/api/Account/Update", updateModel);
        output.WriteLine($"Status: {response.StatusCode}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify profile update
        var profileResponse = await userClient.GetAsync("/api/Account/Profile");
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        
        Assert.NotNull(profile);
        Assert.Equal("Test bio", profile.Bio);
        Assert.NotNull(profile.Metadata);
        Assert.Equal("IT", profile.Metadata["department"]);
        Assert.Equal("EMP001", profile.Metadata["employeeId"]);
    }

    [Fact]
    public async Task User_UpdateProfile_RemoveMetadata_Succeeds()
    {
        // Arrange
        var (user, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.User);
        using var client = factory.CreateAuthenticatedClient(user);

        // Add metadata first
        var addModel = new ProfileUpdateModel
        {
            Metadata = new Dictionary<string, string>
            {
                { "testField", "testValue" }
            }
        };
        await client.PutAsJsonAsync("/api/Account/Update", addModel);

        // Act - remove metadata by setting to empty string
        var removeModel = new ProfileUpdateModel
        {
            Metadata = new Dictionary<string, string>
            {
                { "testField", "" }
            }
        };
        var response = await client.PutAsJsonAsync("/api/Account/Update", removeModel);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify removal
        var profileResponse = await client.GetAsync("/api/Account/Profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileUserInfoModel>();
        
        Assert.NotNull(profile);
        Assert.NotNull(profile.Metadata);
        Assert.DoesNotContain("testField", profile.Metadata.Keys);
    }

    [Fact]
    public async Task NonAdmin_CannotAccessAdminMetadataEndpoints()
    {
        // Arrange
        var (user, _) = await TestDataSeeder.CreateUserWithRoleAsync(factory.Services, Role.User);
        using var client = factory.CreateAuthenticatedClient(user);

        // Act
        var getResponse = await client.GetAsync("/api/Admin/UserMetadata");
        var putResponse = await client.PutAsJsonAsync("/api/Admin/UserMetadata", new List<UserMetadataField>());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
    }
}
