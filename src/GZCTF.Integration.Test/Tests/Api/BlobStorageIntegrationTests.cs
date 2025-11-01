using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for IBlobStorage functionality via Assets API
/// Tests file upload and retrieval operations that work with both local disk and S3 storage
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class BlobStorageIntegrationTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task FileUploadAndRetrieval_ShouldWorkWithBlobStorage()
    {
        // Arrange: Create admin user and authenticate
        var adminPassword = "Admin@Storage123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();

        // Login as admin
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create test file content
        var testContent = "This is a test file for blob storage integration testing.";
        var testFileName = "test-storage-file.txt";
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);

        // Create multipart form data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(contentBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "files", testFileName);

        // Act: Upload file via Assets API
        var uploadResponse = await client.PostAsync("/api/Assets", content);
        output.WriteLine($"Upload Status: {uploadResponse.StatusCode}");

        uploadResponse.EnsureSuccessStatusCode();

        var uploadedFiles = await uploadResponse.Content.ReadFromJsonAsync<List<LocalFile>>();
        Assert.NotNull(uploadedFiles);
        Assert.Single(uploadedFiles);

        var uploadedFile = uploadedFiles[0];
        Assert.Equal(testFileName, uploadedFile.Name);
        Assert.NotEmpty(uploadedFile.Hash);
        Assert.Equal(64, uploadedFile.Hash.Length); // SHA256 hash length

        output.WriteLine($"Uploaded file hash: {uploadedFile.Hash}");
        output.WriteLine($"Uploaded file URL: {uploadedFile.Url()}");

        // Act: Retrieve file via Assets API
        var retrieveResponse = await client.GetAsync(uploadedFile.Url());
        output.WriteLine($"Retrieve Status: {retrieveResponse.StatusCode}");

        retrieveResponse.EnsureSuccessStatusCode();

        var retrievedContent = await retrieveResponse.Content.ReadAsStringAsync();

        // Assert: Verify content matches
        Assert.Equal(testContent, retrievedContent);
        Assert.Equal("text/plain", retrieveResponse.Content.Headers.ContentType?.MediaType);

        output.WriteLine("File upload and retrieval test passed successfully");
    }

    [Fact]
    public async Task FileUploadWithCustomName_ShouldUseProvidedFilename()
    {
        // Arrange: Create admin user and authenticate
        var adminPassword = "Admin@CustomName123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();

        // Login as admin
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create test file content
        var testContent = "Custom filename test content.";
        var originalFileName = "original.txt";
        var customFileName = "custom-filename.txt";
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);

        // Create multipart form data with custom filename query parameter
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(contentBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "files", originalFileName);

        // Act: Upload file with custom filename
        var uploadUrl = $"/api/Assets?filename={Uri.EscapeDataString(customFileName)}";
        var uploadResponse = await client.PostAsync(uploadUrl, content);
        output.WriteLine($"Upload Status: {uploadResponse.StatusCode}");

        uploadResponse.EnsureSuccessStatusCode();

        var uploadedFiles = await uploadResponse.Content.ReadFromJsonAsync<List<LocalFile>>();
        Assert.NotNull(uploadedFiles);
        Assert.Single(uploadedFiles);

        var uploadedFile = uploadedFiles[0];
        Assert.Equal(customFileName, uploadedFile.Name); // Should use custom filename
        Assert.NotEmpty(uploadedFile.Hash);

        output.WriteLine($"File uploaded with custom name: {uploadedFile.Name}");
    }

    [Fact]
    public async Task FileRetrieval_WithWrongFilename_ShouldStillWork()
    {
        // Arrange: Create admin user and authenticate
        var adminPassword = "Admin@WrongName123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();

        // Login as admin
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create test file content
        var testContent = "Wrong filename test content.";
        var testFileName = "wrong-name-test.txt";
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);

        // Create multipart form data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(contentBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "files", testFileName);

        // Upload file
        var uploadResponse = await client.PostAsync("/api/Assets", content);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadedFiles = await uploadResponse.Content.ReadFromJsonAsync<List<LocalFile>>();
        var uploadedFile = uploadedFiles![0];

        // Act: Retrieve file with different filename (API ignores filename, uses hash)
        var wrongFilename = "completely-different-name.pdf";
        var retrieveResponse = await client.GetAsync($"/Assets/{uploadedFile.Hash}/{wrongFilename}");
        output.WriteLine($"Retrieve Status: {retrieveResponse.StatusCode}");

        retrieveResponse.EnsureSuccessStatusCode();

        var retrievedContent = await retrieveResponse.Content.ReadAsStringAsync();

        // Assert: Content should still match even with wrong filename
        Assert.Equal(testContent, retrievedContent);

        output.WriteLine("File retrieval with wrong filename works correctly");
    }
}
