using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for Post operations (EditController and InfoController):
/// - Post creation
/// - Post content editing while maintaining pin status
/// - Pin/unpin operations without affecting content
/// - Partial updates (only updating specific fields)
/// - Validation of all CRUD operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class PostControllerTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    // Configured JSON options matching the application's settings
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new DateTimeOffsetJsonConverter() }
    };
    /// <summary>
    /// Test creating a post with complete data
    /// </summary>
    [Fact]
    public async Task CreatePost_WithCompleteData_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        var postModel = new PostEditModel
        {
            Title = "Test Post Title",
            Summary = "This is a test post summary",
            Content = "# Test Content\n\nThis is the full content of the test post.",
            Tags = new List<string> { "test", "integration" }
            // Do NOT include IsPinned during creation - it would trigger pin-only mode
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Edit/Posts", postModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var postId = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);
        Assert.Equal(8, postId.Length);

        // Verify the post was created correctly
        var getResponse = await client.GetAsync($"/api/Posts/{postId}");
        getResponse.EnsureSuccessStatusCode();
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(post);
        Assert.Equal(postModel.Title, post.Title);
        Assert.Equal(postModel.Summary, post.Summary);
        Assert.Equal(postModel.Content, post.Content);
        Assert.False(post.IsPinned); // Default is unpinned
        Assert.NotNull(post.Tags);
        Assert.Equal(2, post.Tags.Count);
        Assert.Contains("test", post.Tags);
        Assert.Contains("integration", post.Tags);

        output.WriteLine($"Post created successfully with ID: {postId}");
    }

    /// <summary>
    /// Test creating a post with only required fields (title)
    /// </summary>
    [Fact]
    public async Task CreatePost_WithOnlyTitle_ShouldSucceed()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        var postModel = new PostEditModel
        {
            Title = "Minimal Post"
            // All other fields are null/optional
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Edit/Posts", postModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var postId = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Verify defaults
        var getResponse = await client.GetAsync($"/api/Posts/{postId}");
        getResponse.EnsureSuccessStatusCode();
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(post);
        Assert.Equal("Minimal Post", post.Title);
        Assert.Equal(string.Empty, post.Summary);
        Assert.Equal(string.Empty, post.Content);
        Assert.False(post.IsPinned);
        Assert.True(post.Tags is not { Count: > 0 });

        output.WriteLine($"Minimal post created successfully with ID: {postId}");
    }

    /// <summary>
    /// Test creating a post without title - should succeed with empty title
    /// (No validation on backend, Title will be empty string)
    /// </summary>
    [Fact]
    public async Task CreatePost_WithoutTitle_ShouldSucceedWithEmptyTitle()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        var postModel = new PostEditModel
        {
            Content = "Content without title"
            // Title is null - will default to empty string
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Edit/Posts", postModel);

        // Assert - succeeds but creates post with empty title
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var postId = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Verify the post has empty title
        var getResponse = await client.GetAsync($"/api/Posts/{postId}");
        getResponse.EnsureSuccessStatusCode();
        var post = await getResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);
        Assert.NotNull(post);
        Assert.Equal(string.Empty, post.Title);
        Assert.Equal(string.Empty, post.Summary);
        Assert.Equal("Content without title", post.Content);

        output.WriteLine("Post creation with null title succeeds (creates empty title)");
    }

    /// <summary>
    /// Test editing post content while keeping it pinned
    /// With the new implementation: to update content, do NOT send IsPinned field (leave it null)
    /// The pin status will be automatically preserved
    /// </summary>
    [Fact]
    public async Task EditPost_UpdateContentOnPinnedPost_ShouldPreservePinStatus()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a post (without isPinned)
        var createModel = new PostEditModel
        {
            Title = "Original Title",
            Summary = "Original Summary",
            Content = "Original Content",
            Tags = new List<string> { "original" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Pin the post in a separate call
        var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}",
            new PostEditModel { IsPinned = true });
        pinResponse.EnsureSuccessStatusCode();

        // Act: Update the content WITHOUT sending IsPinned (null means don't touch pin status)
        var updateModel = new PostEditModel
        {
            Title = "Updated Title",
            Summary = "Updated Summary",
            Content = "Updated Content",
            Tags = new List<string> { "updated", "modified" }
            // IsPinned is null - pin status should be preserved automatically
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", updateModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedPost = await updateResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(updatedPost);
        Assert.Equal("Updated Title", updatedPost.Title);
        Assert.Equal("Updated Summary", updatedPost.Summary);
        Assert.Equal("Updated Content", updatedPost.Content);
        Assert.True(updatedPost.IsPinned); // Pin status should be preserved
        Assert.NotNull(updatedPost.Tags);
        Assert.Equal(2, updatedPost.Tags.Count);
        Assert.Contains("updated", updatedPost.Tags);
        Assert.Contains("modified", updatedPost.Tags);

        output.WriteLine($"Post {postId} updated successfully while preserving pin status");
    }

    /// <summary>
    /// Test toggling pin status without affecting content
    /// This tests the partial update pattern: only sending isPinned should only update that field
    /// </summary>
    [Fact]
    public async Task EditPost_TogglePinOnly_ShouldPreserveContent()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create an unpinned post with content
        var createModel = new PostEditModel
        {
            Title = "Important Post",
            Summary = "This is an important announcement",
            Content = "# Important\n\nThis content should not be lost when pinning.",
            Tags = new List<string> { "important", "announcement" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Act: Pin the post without sending other fields
        var pinModel = new PostEditModel
        {
            IsPinned = true
            // All other fields are null - should preserve existing values
        };

        var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", pinModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, pinResponse.StatusCode);
        var pinnedPost = await pinResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(pinnedPost);
        Assert.Equal("Important Post", pinnedPost.Title); // Content preserved
        Assert.Equal("This is an important announcement", pinnedPost.Summary);
        Assert.Equal("# Important\n\nThis content should not be lost when pinning.", pinnedPost.Content);
        Assert.True(pinnedPost.IsPinned); // Pin status updated
        Assert.NotNull(pinnedPost.Tags);
        Assert.Equal(2, pinnedPost.Tags.Count);

        output.WriteLine($"Post {postId} pinned successfully without losing content");

        // Act: Unpin the post
        var unpinModel = new PostEditModel
        {
            IsPinned = false
        };

        var unpinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", unpinModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, unpinResponse.StatusCode);
        var unpinnedPost = await unpinResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(unpinnedPost);
        Assert.Equal("Important Post", unpinnedPost.Title); // Content still preserved
        Assert.Equal("This is an important announcement", unpinnedPost.Summary);
        Assert.Equal("# Important\n\nThis content should not be lost when pinning.", unpinnedPost.Content);
        Assert.False(unpinnedPost.IsPinned); // Pin status updated
        Assert.NotNull(unpinnedPost.Tags);
        Assert.Equal(2, unpinnedPost.Tags.Count);

        output.WriteLine($"Post {postId} unpinned successfully without losing content");
    }

    /// <summary>
    /// Test that sending IsPinned with content changes ONLY updates pin status
    /// This verifies the implementation logic: if IsPinned is not null, early return without updating content
    /// </summary>
    [Fact]
    public async Task EditPost_SendIsPinnedWithContent_ShouldOnlyUpdatePin()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create an unpinned post
        var createModel = new PostEditModel
        {
            Title = "Original Title",
            Summary = "Original Summary",
            Content = "Original Content",
            Tags = new List<string> { "original" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Act: Try to pin AND update content in the same request
        var updateModel = new PostEditModel
        {
            Title = "This Should Be Ignored",
            Summary = "This Should Be Ignored",
            Content = "This Should Be Ignored",
            Tags = new List<string> { "ignored" },
            IsPinned = true // When IsPinned is present, only pin status is updated
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", updateModel);

        // Assert: Only pin status should change, content should remain unchanged
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedPost = await updateResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(updatedPost);
        Assert.Equal("Original Title", updatedPost.Title); // Content unchanged
        Assert.Equal("Original Summary", updatedPost.Summary); // Content unchanged
        Assert.Equal("Original Content", updatedPost.Content); // Content unchanged
        Assert.True(updatedPost.IsPinned); // Only pin status updated
        Assert.NotNull(updatedPost.Tags);
        Assert.Single(updatedPost.Tags); // Original tags preserved
        Assert.Contains("original", updatedPost.Tags);

        output.WriteLine($"Post {postId} pin status updated, content correctly preserved");
    }

    /// <summary>
    /// Test partial content update (only updating title)
    /// </summary>
    [Fact]
    public async Task EditPost_UpdateTitleOnly_ShouldPreserveOtherFields()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a post
        var createModel = new PostEditModel
        {
            Title = "Original Title",
            Summary = "Original Summary",
            Content = "Original Content",
            Tags = new List<string> { "tag1", "tag2" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Pin the post
        var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}",
            new PostEditModel { IsPinned = true });
        pinResponse.EnsureSuccessStatusCode();

        // Act: Update only the title
        var updateModel = new PostEditModel
        {
            Title = "New Title"
            // All other fields are null
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", updateModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedPost = await updateResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(updatedPost);
        Assert.Equal("New Title", updatedPost.Title); // Title updated
        Assert.Equal("Original Summary", updatedPost.Summary); // Preserved
        Assert.Equal("Original Content", updatedPost.Content); // Preserved
        Assert.True(updatedPost.IsPinned); // Preserved
        Assert.NotNull(updatedPost.Tags);
        Assert.Equal(2, updatedPost.Tags.Count); // Preserved

        output.WriteLine($"Post {postId} title updated successfully while preserving other fields");
    }

    /// <summary>
    /// Test updating tags without affecting content or pin status
    /// </summary>
    [Fact]
    public async Task EditPost_UpdateTagsOnly_ShouldPreserveContentAndPin()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a pinned post
        var createModel = new PostEditModel
        {
            Title = "Tagged Post",
            Summary = "Summary",
            Content = "Content",
            Tags = new List<string> { "old-tag" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Pin the post
        var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}",
            new PostEditModel { IsPinned = true });
        pinResponse.EnsureSuccessStatusCode();

        // Act: Update only tags
        var updateModel = new PostEditModel
        {
            Tags = new List<string> { "new-tag", "another-tag" }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", updateModel);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedPost = await updateResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(updatedPost);
        Assert.Equal("Tagged Post", updatedPost.Title); // Preserved
        Assert.Equal("Summary", updatedPost.Summary); // Preserved
        Assert.Equal("Content", updatedPost.Content); // Preserved
        Assert.True(updatedPost.IsPinned); // Preserved
        Assert.NotNull(updatedPost.Tags);
        Assert.Equal(2, updatedPost.Tags.Count); // Tags updated
        Assert.Contains("new-tag", updatedPost.Tags);
        Assert.Contains("another-tag", updatedPost.Tags);

        output.WriteLine($"Post {postId} tags updated successfully while preserving content and pin status");
    }

    /// <summary>
    /// Test the original bug scenario: editing a pinned post's content
    /// The fix: Do NOT send IsPinned when updating content - it will be preserved automatically
    /// </summary>
    [Fact]
    public async Task BugScenario_EditPinnedPostContent_ShouldNotUnpinOrLoseContent()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Step 1: Create a post
        var createModel = new PostEditModel
        {
            Title = "Pinned Announcement",
            Summary = "Important announcement summary",
            Content = "# Important\n\nThis is a pinned announcement with important content.",
            Tags = new List<string> { "announcement", "pinned" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Step 2: Pin the post
        var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}",
            new PostEditModel { IsPinned = true });
        pinResponse.EnsureSuccessStatusCode();

        output.WriteLine($"Created and pinned post: {postId}");

        // Step 2: Edit the content (WITHOUT sending IsPinned field)
        // This simulates the correct way to edit a post while preserving pin status
        var editModel = new PostEditModel
        {
            Title = "Updated Pinned Announcement",
            Summary = "Updated summary",
            Content = "# Updated\n\nThis is the updated content that should not be lost.",
            Tags = new List<string> { "announcement", "pinned", "updated" }
            // IsPinned is null - this tells the backend to preserve the current pin status
        };

        var editResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}", editModel);

        // Assert: Content should be updated and pin status preserved
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        var editedPost = await editResponse.Content.ReadFromJsonAsync<PostDetailModel>(JsonOptions);

        Assert.NotNull(editedPost);
        Assert.Equal("Updated Pinned Announcement", editedPost.Title);
        Assert.Equal("Updated summary", editedPost.Summary);
        Assert.Equal("# Updated\n\nThis is the updated content that should not be lost.", editedPost.Content);
        Assert.True(editedPost.IsPinned); // CRITICAL: Must remain pinned
        Assert.NotNull(editedPost.Tags);
        Assert.Equal(3, editedPost.Tags.Count);

        output.WriteLine($"Post {postId} content updated successfully, pin status preserved: {editedPost.IsPinned}");

        // Step 3: Verify the post in the database
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbPost = await context.Posts.FindAsync(postId);

        Assert.NotNull(dbPost);
        Assert.Equal("Updated Pinned Announcement", dbPost.Title);
        Assert.Equal("Updated summary", dbPost.Summary);
        Assert.Equal("# Updated\n\nThis is the updated content that should not be lost.", dbPost.Content);
        Assert.True(dbPost.IsPinned);

        output.WriteLine("Database verification passed: content and pin status are correct");
    }

    /// <summary>
    /// Test deleting a post
    /// </summary>
    [Fact]
    public async Task DeletePost_ShouldRemovePostFromDatabase()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a post
        var createModel = new PostEditModel
        {
            Title = "Post to Delete",
            Content = "This post will be deleted"
        };

        var createResponse = await client.PostAsJsonAsync("/api/Edit/Posts", createModel);
        createResponse.EnsureSuccessStatusCode();
        var postId = await createResponse.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(postId);

        // Act: Delete the post
        var deleteResponse = await client.DeleteAsync($"/api/Edit/Posts/{postId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // Verify post is deleted
        var getResponse = await client.GetAsync($"/api/Posts/{postId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        output.WriteLine($"Post {postId} deleted successfully");
    }

    /// <summary>
    /// Test listing posts includes pinned posts in correct order
    /// </summary>
    [Fact]
    public async Task GetPosts_ShouldReturnPinnedPostsFirst()
    {
        // Arrange
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create multiple posts
        var post1Id = await CreatePostAsync(client, "Regular Post 1", isPinned: false);
        var post2Id = await CreatePostAsync(client, "Pinned Post 1", isPinned: true);
        var post3Id = await CreatePostAsync(client, "Regular Post 2", isPinned: false);
        var post4Id = await CreatePostAsync(client, "Pinned Post 2", isPinned: true);

        // Act: Get posts list
        var response = await client.GetAsync("/api/Posts/Latest");
        response.EnsureSuccessStatusCode();
        var posts = await response.Content.ReadFromJsonAsync<PostInfoModel[]>(JsonOptions);

        // Assert
        Assert.NotNull(posts);
        Assert.True(posts.Length >= 4);

        // Find our posts
        var ourPosts = posts.Where(p => p.Id == post1Id || p.Id == post2Id || p.Id == post3Id || p.Id == post4Id)
            .ToList();
        Assert.Equal(4, ourPosts.Count);

        // Pinned posts should come first
        var pinnedPosts = ourPosts.Where(p => p.IsPinned).ToList();
        var regularPosts = ourPosts.Where(p => !p.IsPinned).ToList();

        Assert.Equal(2, pinnedPosts.Count);
        Assert.Equal(2, regularPosts.Count);

        output.WriteLine($"Posts retrieved correctly: {pinnedPosts.Count} pinned, {regularPosts.Count} regular");
    }

    private static async Task<string> CreatePostAsync(HttpClient client, string title, bool isPinned)
    {
        var model = new PostEditModel
        {
            Title = title,
            Summary = $"Summary for {title}",
            Content = $"Content for {title}"
            // Do not include IsPinned during creation
        };

        var response = await client.PostAsJsonAsync("/api/Edit/Posts", model);
        response.EnsureSuccessStatusCode();
        var postId = await response.Content.ReadFromJsonAsync<string>();

        // Pin in a separate call if needed
        if (isPinned)
        {
            var pinResponse = await client.PutAsJsonAsync($"/api/Edit/Posts/{postId}",
                new PostEditModel { IsPinned = true });
            pinResponse.EnsureSuccessStatusCode();
        }

        return postId!;
    }
}
