using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Transfer;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for game export and import functionality
/// Tests the complete export/import cycle with complex game configurations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameExportImportTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    /// <summary>
    /// Test exporting and importing a complex game with all features:
    /// - Multiple divisions with different configurations
    /// - Multiple challenges (static attachment, static container, dynamic container)
    /// - Challenge attachments
    /// - Blood bonus configuration
    /// - Writeup configuration
    /// - Game poster
    /// - Challenge hints
    /// </summary>
    [Fact]
    public async Task ExportImport_ComplexGame_ShouldPreserveAllData()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a complex game with all features
        var originalGame = await CreateComplexGameAsync();
        output.WriteLine($"Created complex game with ID: {originalGame.Id}");

        // Act 1: Export the game
        output.WriteLine($"Exporting game {originalGame.Id}...");
        var exportResponse = await adminClient.PostAsync($"/api/Edit/Games/{originalGame.Id}/Export", null);
        exportResponse.EnsureSuccessStatusCode();

        Assert.Equal("application/zip", exportResponse.Content.Headers.ContentType?.MediaType);

        // Download and validate the exported ZIP
        var exportedZipBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(exportedZipBytes);
        output.WriteLine($"Exported ZIP size: {exportedZipBytes.Length} bytes");

        // Validate ZIP structure
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"test-export-{Guid.NewGuid()}.zip");
        await File.WriteAllBytesAsync(tempZipPath, exportedZipBytes);

        try
        {
            await ValidateExportedZipStructure(tempZipPath, originalGame);

            // Act 2: Import the game back
            output.WriteLine("Importing game from ZIP...");
            await using var fileStream = File.OpenRead(tempZipPath);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", "game-import.zip");

            var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);
            importResponse.EnsureSuccessStatusCode();

            var importedGameId = await importResponse.Content.ReadFromJsonAsync<int>();
            Assert.True(importedGameId > 0);
            output.WriteLine($"Imported game with new ID: {importedGameId}");

            // Assert: Verify imported game data matches original
            await ValidateImportedGame(originalGame, importedGameId);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    /// <summary>
    /// Test export validation - should return 404 for non-existent game
    /// </summary>
    [Fact]
    public async Task ExportGame_NonExistentGame_ShouldReturn404()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to export a non-existent game
        var exportResponse = await adminClient.PostAsync("/api/Edit/Games/999999/Export", null);

        // Assert: Should return 404
        Assert.Equal(HttpStatusCode.NotFound, exportResponse.StatusCode);
    }

    /// <summary>
    /// Test import validation - should reject invalid file types
    /// </summary>
    [Fact]
    public async Task ImportGame_InvalidFileType_ShouldReturnBadRequest()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to import a non-ZIP file
        using var content = new MultipartFormDataContent();
        var textContent = new StringContent("This is not a ZIP file");
        textContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(textContent, "file", "invalid.txt");

        var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);

        // Assert: Should return bad request
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
    }

    /// <summary>
    /// Test import validation - should reject empty files
    /// </summary>
    [Fact]
    public async Task ImportGame_EmptyFile_ShouldReturnBadRequest()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to import an empty file
        using var content = new MultipartFormDataContent();
        var emptyContent = new ByteArrayContent([]);
        emptyContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(emptyContent, "file", "empty.zip");

        var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);

        // Assert: Should return bad request
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
    }

    /// <summary>
    /// Test import validation - should reject corrupted ZIP files
    /// </summary>
    [Fact]
    public async Task ImportGame_CorruptedZip_ShouldReturnBadRequest()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to import a corrupted ZIP (just random bytes with .zip extension)
        using var content = new MultipartFormDataContent();
        var corruptedContent = new ByteArrayContent("PK\x03\x04CORRUPTED_DATA"u8.ToArray());
        corruptedContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(corruptedContent, "file", "corrupted.zip");

        var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);

        // Assert: Should return bad request for corrupted ZIP
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
    }

    /// <summary>
    /// Test that unauthorized users cannot export games
    /// </summary>
    [Fact]
    public async Task ExportGame_AsUnauthorizedUser_ShouldReturn403()
    {
        // Arrange: Create regular user (not admin)
        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword, role: Role.User);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Test Game");

        using var userClient = factory.CreateClient();

        var loginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act: Try to export as regular user
        var exportResponse = await userClient.PostAsync($"/api/Edit/Games/{game.Id}/Export", null);

        // Assert: Should return 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, exportResponse.StatusCode);
    }

    /// <summary>
    /// Test blob reference counting during export/import cycle
    /// Verifies that:
    /// - Attachments are correctly tracked in blob repository
    /// - Reference counts increase when importing the same file
    /// - Files are properly shared between original and imported games
    /// </summary>
    [Fact]
    public async Task ExportImport_BlobReferenceCount_ShouldBeTrackedCorrectly()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@BlobTest123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a game with attachments
        var gameWithBlobs = await CreateGameWithAttachmentsAsync();
        output.WriteLine($"Created game {gameWithBlobs.Id} with attachments");

        // Get initial blob reference counts
        var initialBlobCounts = await GetBlobReferenceCounts();
        output.WriteLine($"Initial blob count: {initialBlobCounts.Count} files");

        foreach (var (hash, refCount) in initialBlobCounts)
        {
            output.WriteLine($"  Blob {hash[..8]}: RefCount={refCount}");
        }

        // Count how many times each blob is used in the game (for verification after import)
        var blobUsageCount = await CountBlobUsageInGame(gameWithBlobs.Id);
        foreach (var (hash, usageCount) in blobUsageCount)
        {
            output.WriteLine($"  Blob {hash[..8]} used {usageCount} time(s) in game");
        }

        // Act 1: Export the game
        var exportResponse = await adminClient.PostAsync($"/api/Edit/Games/{gameWithBlobs.Id}/Export", null);
        exportResponse.EnsureSuccessStatusCode();

        var exportedZipBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        output.WriteLine($"Exported game to ZIP ({exportedZipBytes.Length} bytes)");

        // Verify reference counts haven't changed after export
        var afterExportBlobCounts = await GetBlobReferenceCounts();
        Assert.Equal(initialBlobCounts.Count, afterExportBlobCounts.Count);

        foreach (var (hash, initialCount) in initialBlobCounts)
        {
            Assert.True(afterExportBlobCounts.ContainsKey(hash), $"Blob {hash[..8]} should still exist");
            Assert.Equal(initialCount, afterExportBlobCounts[hash]);
        }

        output.WriteLine("Blob reference counts unchanged after export ✓");

        // Act 2: Import the game
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"blob-test-{Guid.NewGuid()}.zip");
        await File.WriteAllBytesAsync(tempZipPath, exportedZipBytes);

        try
        {
            await using var fileStream = File.OpenRead(tempZipPath);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", "game-import.zip");

            var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);
            importResponse.EnsureSuccessStatusCode();

            var importedGameId = await importResponse.Content.ReadFromJsonAsync<int>();
            output.WriteLine($"Imported game with ID {importedGameId}");

            // Assert: Verify reference counts increased for shared files
            var afterImportBlobCounts = await GetBlobReferenceCounts();

            // Same number of unique files (no duplicates created)
            Assert.Equal(initialBlobCounts.Count, afterImportBlobCounts.Count);

            // Each file should have reference count increased by its usage count in the game
            foreach (var (hash, initialCount) in initialBlobCounts)
            {
                Assert.True(afterImportBlobCounts.ContainsKey(hash), $"Blob {hash[..8]} should exist after import");
                var newCount = afterImportBlobCounts[hash];
                var expectedIncrement = blobUsageCount.TryGetValue(hash, out var count) ? count : 0u;
                var expectedCount = initialCount + expectedIncrement;
                Assert.Equal(expectedCount, newCount);
                output.WriteLine($"  Blob {hash[..8]}: RefCount {initialCount} → {newCount} (used {expectedIncrement}x) ✓");
            }

            output.WriteLine("All blob reference counts correctly incremented ✓");

            // Verify both games use the same physical files
            var originalGame = await GetGameWithChallenges(gameWithBlobs.Id);
            var importedGame = await GetGameWithChallenges(importedGameId);

            Assert.NotNull(originalGame);
            Assert.NotNull(importedGame);

            // Compare attachment hashes
            foreach (var originalChallenge in originalGame.Challenges)
            {
                if (originalChallenge.Attachment is null)
                    continue;

                var importedChallenge = importedGame.Challenges.FirstOrDefault(c => c.Title == originalChallenge.Title);
                Assert.NotNull(importedChallenge);
                Assert.NotNull(importedChallenge.Attachment);

                // Should reference the same blob file
                Assert.Equal(originalChallenge.Attachment.LocalFile?.Hash,
                    importedChallenge.Attachment.LocalFile?.Hash);

                output.WriteLine($"Challenge '{originalChallenge.Title}' shares blob {originalChallenge.Attachment.LocalFile?.Hash[..8]} ✓");
            }

            output.WriteLine("Blob sharing verified between original and imported games ✓");
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Create a complex game with all features for comprehensive testing
    /// </summary>
    private async Task<Game> CreateComplexGameAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        // Create game with full configuration
        var game = new Game
        {
            Title = $"Complex Export Test Game {Guid.NewGuid().ToString("N")[..8]}",
            Summary = "A comprehensive test game for export/import",
            Content = "# Test Game\n\nThis is a **complex** game with all features enabled.\n\n## Features\n- Multiple divisions\n- Various challenge types\n- Blood bonus\n- Writeup requirements",
            Hidden = false,
            PracticeMode = false,
            AcceptWithoutReview = true,
            InviteCode = $"TEST{Guid.NewGuid().ToString("N")[..8]}",
            TeamMemberCountLimit = 5,
            ContainerCountLimit = 3,
            StartTimeUtc = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(7),
            WriteupRequired = true,
            WriteupDeadline = DateTimeOffset.UtcNow.AddDays(14),
            BloodBonus = new BloodBonus((50L << 20) + (30L << 10) + 10L) // First=50, Second=30, Third=10
        };

        await context.Games.AddAsync(game);
        await context.SaveChangesAsync();

        // Create divisions
        var division1 = new Division
        {
            GameId = game.Id,
            Name = "Professional",
            DefaultPermissions = GamePermission.All
        };

        var division2 = new Division
        {
            GameId = game.Id,
            Name = "Student",
            DefaultPermissions = GamePermission.All
        };

        await context.Divisions.AddRangeAsync(division1, division2);
        await context.SaveChangesAsync();

        // Create challenges with different types

        // 1. Static attachment challenge
        var staticChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Crypto Mystery",
            Content = "## Challenge Description\n\nFind the hidden message in the encrypted file.\n\n**Hint**: Caesar cipher with offset 13",
            Category = ChallengeCategory.Crypto,
            Type = ChallengeType.StaticAttachment,
            Hints = ["The offset is a prime number", "ROT13 is your friend"],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 1000,
            MinScoreRate = 0.5,
            Difficulty = 3
        };

        var staticFlag = new FlagContext
        {
            Flag = "flag{st4t1c_crypt0_1s_fun}",
            Challenge = staticChallenge
        };
        staticChallenge.Flags.Add(staticFlag);

        await challengeRepo.CreateChallenge(game, staticChallenge, CancellationToken.None);

        // 2. Static container challenge
        var staticContainerChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Web Exploitation",
            Content = "## Challenge\n\nFind and exploit the SQL injection vulnerability.\n\nAccess: `nc challenge.ctf 8080`",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticContainer,
            Hints = ["Check the login form", "UNION SELECT is powerful"],
            IsEnabled = true,
            SubmissionLimit = 10,
            OriginalScore = 1500,
            MinScoreRate = 0.7,
            Difficulty = 5,
            ContainerImage = "web-challenge:latest",
            CPUCount = 1,
            MemoryLimit = 256,
            StorageLimit = 128,
            ContainerExposePort = 80
        };

        var staticContainerFlag = new FlagContext
        {
            Flag = "flag{sql_1nj3ct10n_m4st3r}",
            Challenge = staticContainerChallenge
        };
        staticContainerChallenge.Flags.Add(staticContainerFlag);

        await challengeRepo.CreateChallenge(game, staticContainerChallenge, CancellationToken.None);

        // 3. Dynamic container challenge
        var dynamicChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Pwn The Binary",
            Content = "## Binary Exploitation\n\nExploit the buffer overflow to get shell access.\n\nEach team gets a unique instance with a dynamic flag.",
            Category = ChallengeCategory.Pwn,
            Type = ChallengeType.DynamicContainer,
            Hints = ["Check the stack carefully", "NX is disabled"],
            IsEnabled = true,
            SubmissionLimit = 50,
            OriginalScore = 2000,
            MinScoreRate = 0.8,
            Difficulty = 8,
            ContainerImage = "pwn-challenge:latest",
            CPUCount = 1,
            MemoryLimit = 512,
            StorageLimit = 256,
            ContainerExposePort = 9999,
            FlagTemplate = "flag{dyn4m1c_[GUID]}",
        };

        await challengeRepo.CreateChallenge(game, dynamicChallenge, CancellationToken.None);

        // 4. Static attachment challenge with multiple flags
        var multiFlag = new GameChallenge
        {
            GameId = game.Id,
            Title = "Multi-Stage Reverse",
            Content = "## Multi-Stage Challenge\n\nThis challenge has multiple flags. Find them all!",
            Category = ChallengeCategory.Reverse,
            Type = ChallengeType.StaticAttachment,
            Hints = ["Start with strings", "Check the resources", "Don't forget the encryption key"],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 1200,
            MinScoreRate = 0.6,
            Difficulty = 6
        };

        multiFlag.Flags.Add(new FlagContext { Flag = "flag{stage_1_c0mpl3te}", Challenge = multiFlag });
        multiFlag.Flags.Add(new FlagContext { Flag = "flag{stage_2_d0ne}", Challenge = multiFlag });
        multiFlag.Flags.Add(new FlagContext { Flag = "flag{final_st4g3_w1n}", Challenge = multiFlag });

        await challengeRepo.CreateChallenge(game, multiFlag);

        // Add division-specific challenge configurations
        var div1Config1 = new DivisionChallengeConfig
        {
            DivisionId = division1.Id,
            ChallengeId = staticChallenge.Id,
            Permissions = GamePermission.All
        };

        var div1Config2 = new DivisionChallengeConfig
        {
            DivisionId = division1.Id,
            ChallengeId = staticContainerChallenge.Id,
            Permissions = GamePermission.All
        };

        var div2Config1 = new DivisionChallengeConfig
        {
            DivisionId = division2.Id,
            ChallengeId = staticChallenge.Id,
            Permissions = GamePermission.All
        };

        await context.AddRangeAsync(div1Config1, div1Config2, div2Config1);
        await context.SaveChangesAsync();

        // Reload game with all relationships
        var reloadedGame = await context.Games
            .Include(g => g.Divisions!)
            .Include(g => g.Challenges)
            .ThenInclude(c => c.Flags)
            .FirstOrDefaultAsync(g => g.Id == game.Id);

        return reloadedGame!;
    }

    /// <summary>
    /// Validate the structure and content of exported ZIP
    /// </summary>
    private async Task ValidateExportedZipStructure(string zipPath, Game originalGame)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        // Check required files exist
        Assert.NotNull(archive.GetEntry("manifest.json"));
        Assert.NotNull(archive.GetEntry("game.json"));

        // Check challenges directory exists
        var challengeEntries = archive.Entries.Where(e => e.FullName.StartsWith("challenges/challenge-") && e.FullName.EndsWith(".json")).ToList();
        Assert.NotEmpty(challengeEntries);

        // Validate manifest
        var manifestEntry = archive.GetEntry("manifest.json")!;
        await using var manifestStream = manifestEntry.Open();
        using var manifestReader = new StreamReader(manifestStream);
        var manifestJson = await manifestReader.ReadToEndAsync();
        var manifest = TransferHelper.FromJson<TransferManifest>(manifestJson);

        Assert.NotNull(manifest);
        Assert.Equal("1.0", manifest.Version);
        Assert.Equal("GZCTF-GAME", manifest.Format);
        Assert.Equal(originalGame.Id, manifest.Game.Id);
        Assert.Equal(originalGame.Title, manifest.Game.Title);
        Assert.NotNull(manifest.Statistics);
        Assert.True(manifest.Statistics.ChallengeCount >= 4); // We created 4 challenges

        output.WriteLine($"Manifest validated: {manifest.Statistics.ChallengeCount} challenges, " +
                        $"{manifest.Statistics.DivisionCount} divisions");

        // Validate game.json
        var gameEntry = archive.GetEntry("game.json")!;
        await using var gameStream = gameEntry.Open();
        using var gameReader = new StreamReader(gameStream);
        var gameJson = await gameReader.ReadToEndAsync();
        var transferGame = TransferHelper.FromJson<TransferGame>(gameJson);

        Assert.NotNull(transferGame);
        Assert.Equal(originalGame.Title, transferGame.Title);
        Assert.Equal(originalGame.Summary, transferGame.Summary);
        Assert.Equal(originalGame.Content, transferGame.Content);
        Assert.Equal(originalGame.Hidden, transferGame.Hidden);
        Assert.Equal(originalGame.PracticeMode, transferGame.PracticeMode);
        Assert.Equal(originalGame.InviteCode, transferGame.InviteCode);
        Assert.NotNull(transferGame.BloodBonus);
        Assert.Equal(50, transferGame.BloodBonus.First);
        Assert.NotNull(transferGame.Writeup);
        Assert.True(transferGame.Writeup.Required);

        // Validate individual challenge files
        var transferChallenges = await Task.WhenAll(
            challengeEntries.Select(async challengeEntry =>
            {
                await using var challengeStream = challengeEntry.Open();
                using var challengeReader = new StreamReader(challengeStream);
                var challengeJson = await challengeReader.ReadToEndAsync();
                var transferChallenge = TransferHelper.FromJson<TransferChallenge>(challengeJson);
                Assert.NotNull(transferChallenge);
                return transferChallenge;
            })
        );

        Assert.True(transferChallenges.Length >= 4);

        // Validate specific challenge details
        var cryptoChallenge = transferChallenges.FirstOrDefault(c => c.Title == "Crypto Mystery");
        Assert.NotNull(cryptoChallenge);
        Assert.Equal(ChallengeCategory.Crypto, cryptoChallenge.Category);
        Assert.Equal(ChallengeType.StaticAttachment, cryptoChallenge.Type);
        Assert.NotNull(cryptoChallenge.Hints);
        Assert.Equal(2, cryptoChallenge.Hints.Count);
        Assert.Equal(1000, cryptoChallenge.Scoring.Original);

        var pwnChallenge = transferChallenges.FirstOrDefault(c => c.Title == "Pwn The Binary");
        Assert.NotNull(pwnChallenge);
        Assert.Equal(ChallengeCategory.Pwn, pwnChallenge.Category);
        Assert.Equal(ChallengeType.DynamicContainer, pwnChallenge.Type);
        Assert.NotNull(pwnChallenge.Container);
        Assert.Equal(512, pwnChallenge.Container.MemoryLimit);

        var multiStage = transferChallenges.FirstOrDefault(c => c.Title == "Multi-Stage Reverse");
        Assert.NotNull(multiStage);
        Assert.NotNull(multiStage.Flags.Static);
        Assert.Equal(3, multiStage.Flags.Static.Count); // 3 flags

        output.WriteLine($"Validated {transferChallenges.Length} challenges in exported ZIP");
    }

    /// <summary>
    /// Validate imported game matches original data
    /// </summary>
    private async Task ValidateImportedGame(Game originalGame, int importedGameId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Use AsSplitQuery to avoid cartesian product from multiple includes
        var importedGame = await context.Games
            .AsSplitQuery()
            .Include(g => g.Divisions!)
            .ThenInclude(d => d.ChallengeConfigs)
            .Include(g => g.Challenges)
            .ThenInclude(c => c.Flags)
            .FirstOrDefaultAsync(g => g.Id == importedGameId);

        Assert.NotNull(importedGame);

        // Validate game properties
        Assert.Equal(originalGame.Title, importedGame.Title);
        Assert.Equal(originalGame.Summary, importedGame.Summary);
        Assert.Equal(originalGame.Content, importedGame.Content);

        // Note: Import service applies safety defaults:
        // - Hidden = true (prevent accidental exposure)
        // - PracticeMode = false (require manual enablement)
        // - InviteCode = empty (generate new code)
        Assert.True(importedGame.Hidden); // Import as hidden for safety
        Assert.False(importedGame.PracticeMode); // Import with practice mode disabled for safety

        Assert.Equal(originalGame.AcceptWithoutReview, importedGame.AcceptWithoutReview);
        Assert.Equal(originalGame.TeamMemberCountLimit, importedGame.TeamMemberCountLimit);
        Assert.Equal(originalGame.ContainerCountLimit, importedGame.ContainerCountLimit);

        // Blood bonus
        Assert.Equal(originalGame.BloodBonus.FirstBlood, importedGame.BloodBonus.FirstBlood);
        Assert.Equal(originalGame.BloodBonus.SecondBlood, importedGame.BloodBonus.SecondBlood);
        Assert.Equal(originalGame.BloodBonus.ThirdBlood, importedGame.BloodBonus.ThirdBlood);

        // Writeup
        Assert.Equal(originalGame.WriteupRequired, importedGame.WriteupRequired);

        // Validate divisions
        Assert.NotNull(importedGame.Divisions);
        Assert.Equal(originalGame.Divisions!.Count, importedGame.Divisions.Count);

        // Validate division names are preserved
        foreach (var originalDivision in originalGame.Divisions!)
        {
            var importedDivision = importedGame.Divisions!.FirstOrDefault(d => d.Name == originalDivision.Name);
            Assert.NotNull(importedDivision);
            Assert.Equal(originalDivision.Name, importedDivision.Name);
            output.WriteLine($"Division '{originalDivision.Name}' preserved correctly ✓");
        }

        // Validate challenges count and basic properties
        Assert.NotNull(importedGame.Challenges);
        Assert.Equal(originalGame.Challenges.Count, importedGame.Challenges.Count);

        output.WriteLine($"Imported game validated: {importedGame.Challenges.Count} challenges, " +
                        $"{importedGame.Divisions.Count} divisions");

        // Validate specific challenges
        foreach (var originalChallenge in originalGame.Challenges)
        {
            var importedChallenge = importedGame.Challenges.FirstOrDefault(c => c.Title == originalChallenge.Title);
            Assert.NotNull(importedChallenge);

            Assert.Equal(originalChallenge.Content, importedChallenge.Content);
            Assert.Equal(originalChallenge.Category, importedChallenge.Category);
            Assert.Equal(originalChallenge.Type, importedChallenge.Type);
            Assert.Equal(originalChallenge.OriginalScore, importedChallenge.OriginalScore);
            Assert.Equal(originalChallenge.MinScoreRate, importedChallenge.MinScoreRate);
            Assert.Equal(originalChallenge.Difficulty, importedChallenge.Difficulty);
            Assert.Equal(originalChallenge.FlagTemplate, importedChallenge.FlagTemplate);

            // Validate hints
            if (originalChallenge.Hints is not null)
            {
                Assert.NotNull(importedChallenge.Hints);
                Assert.Equal(originalChallenge.Hints.Count, importedChallenge.Hints.Count);
            }

            // Validate flags count (exact flag values may differ for dynamic flags)
            Assert.Equal(originalChallenge.Flags.Count, importedChallenge.Flags.Count);

            // Validate container config for container challenges
            if (originalChallenge.Type.IsContainer())
            {
                Assert.Equal(originalChallenge.ContainerImage, importedChallenge.ContainerImage);
                Assert.Equal(originalChallenge.CPUCount, importedChallenge.CPUCount);
                Assert.Equal(originalChallenge.MemoryLimit, importedChallenge.MemoryLimit);
                Assert.Equal(originalChallenge.ContainerExposePort, importedChallenge.ContainerExposePort);
            }
        }
    }

    /// <summary>
    /// Create a game with multiple challenges containing blob attachments
    /// </summary>
    private async Task<Game> CreateGameWithAttachmentsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var blobRepo = scope.ServiceProvider.GetRequiredService<IBlobRepository>();

        // Create game
        var game = new Game
        {
            Title = $"Blob Test Game {Guid.NewGuid().ToString("N")[..8]}",
            Summary = "Game with blob attachments for testing reference counting",
            Content = "# Blob Reference Test\n\nThis game tests blob reference counting during export/import.",
            Hidden = false,
            AcceptWithoutReview = true,
            TeamMemberCountLimit = 4,
            ContainerCountLimit = 2,
            StartTimeUtc = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await context.Games.AddAsync(game);
        await context.SaveChangesAsync();

        // Create test blob files with different content
        var testFiles = new[]
        {
            ("crypto-data.txt", "This is a crypto challenge file with sensitive data."),
            ("web-exploit.zip", "PK\x03\x04 fake zip content for testing"),
            ("misc-flag.pdf", "%PDF-1.4 fake PDF header for testing")
        };

        var createdBlobs = new List<LocalFile>();

        foreach (var (fileName, content) in testFiles)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var blob = await blobRepo.CreateOrUpdateBlobFromStream(fileName, stream);
            createdBlobs.Add(blob);
            output.WriteLine($"Created blob: {blob.Hash[..8]} ({fileName})");
        }

        // Create challenges with these attachments

        // Challenge 1: Crypto with attachment
        var cryptoChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Crypto Challenge with Blob",
            Content = "Decrypt the file to find the flag.",
            Category = ChallengeCategory.Crypto,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 500,
            MinScoreRate = 0.5,
            Difficulty = 3
        };

        var cryptoFlag = new FlagContext
        {
            Flag = "flag{crypt0_bl0b_t3st}",
            Challenge = cryptoChallenge
        };
        cryptoChallenge.Flags.Add(cryptoFlag);

        cryptoChallenge = await challengeRepo.CreateChallenge(game, cryptoChallenge);

        // Add attachment
        cryptoChallenge.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = createdBlobs[0]
        };
        await context.SaveChangesAsync();

        // Challenge 2: Web with attachment
        var webChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Web Challenge with Blob",
            Content = "Exploit the web application source code.",
            Category = ChallengeCategory.Web,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 800,
            MinScoreRate = 0.6,
            Difficulty = 5
        };

        var webFlag = new FlagContext
        {
            Flag = "flag{w3b_bl0b_t3st}",
            Challenge = webChallenge
        };
        webChallenge.Flags.Add(webFlag);

        webChallenge = await challengeRepo.CreateChallenge(game, webChallenge);

        // Add attachment
        webChallenge.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = createdBlobs[1]
        };
        await context.SaveChangesAsync();

        // Challenge 3: Misc with shared attachment (reuse first blob)
        var miscChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Misc Challenge with Shared Blob",
            Content = "This challenge shares the same file as the crypto challenge.",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 300,
            MinScoreRate = 0.5,
            Difficulty = 2
        };

        var miscFlag = new FlagContext
        {
            Flag = "flag{m1sc_shar3d_bl0b}",
            Challenge = miscChallenge
        };
        miscChallenge.Flags.Add(miscFlag);

        miscChallenge = await challengeRepo.CreateChallenge(game, miscChallenge);

        // Add attachment (shared blob - should increment ref count)
        await blobRepo.IncrementBlobReference(createdBlobs[0].Hash);
        miscChallenge.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = createdBlobs[0]
        };
        await context.SaveChangesAsync();

        // Challenge 4: Forensics with third blob
        var forensicsChallenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Forensics PDF Analysis",
            Content = "Analyze the PDF file to find hidden data.",
            Category = ChallengeCategory.Forensics,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 600,
            MinScoreRate = 0.5,
            Difficulty = 4
        };

        var forensicsFlag = new FlagContext
        {
            Flag = "flag{f0r3ns1cs_pdf_bl0b}",
            Challenge = forensicsChallenge
        };
        forensicsChallenge.Flags.Add(forensicsFlag);

        forensicsChallenge = await challengeRepo.CreateChallenge(game, forensicsChallenge);

        // Add attachment using third blob
        forensicsChallenge.Attachment = new Attachment
        {
            Type = FileType.Local,
            LocalFile = createdBlobs[2]
        };
        await context.SaveChangesAsync();

        output.WriteLine($"Created game with {game.Challenges.Count} challenges and blob attachments");

        return game;
    }

    /// <summary>
    /// Get current blob reference counts for all files
    /// </summary>
    private async Task<Dictionary<string, uint>> GetBlobReferenceCounts()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var blobs = await context.Files.ToDictionaryAsync(f => f.Hash, f => f.ReferenceCount);
        return blobs;
    }

    /// <summary>
    /// Count how many times each blob is used in a specific game
    /// </summary>
    private async Task<Dictionary<string, uint>> CountBlobUsageInGame(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var usageCounts = await context.GameChallenges
            .Where(c => c.GameId == gameId && c.Attachment != null && c.Attachment.LocalFile != null)
            .GroupBy(c => c.Attachment!.LocalFile!.Hash)
            .Select(g => new { Hash = g.Key, Count = (uint)g.Count() })
            .ToDictionaryAsync(x => x.Hash, x => x.Count);

        return usageCounts;
    }

    /// <summary>
    /// Test that attachment file names are correctly preserved during export/import
    /// </summary>
    [Fact]
    public async Task ExportImport_AttachmentFileNames_ShouldBePreserved()
    {
        // Arrange: Create admin user
        var adminPassword = "Admin@Pass123";
        var adminUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using var adminClient = factory.CreateClient();

        // Admin login
        var loginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = adminUser.UserName, Password = adminPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Create a game with challenges that have attachments
        var originalGame = await CreateGameWithNamedAttachmentsAsync();
        output.WriteLine($"Created game with attachments: {originalGame.Id}");

        // Act 1: Export the game
        output.WriteLine("Exporting game with attachments...");
        var exportResponse = await adminClient.PostAsync($"/api/Edit/Games/{originalGame.Id}/Export", null);
        exportResponse.EnsureSuccessStatusCode();

        var exportedZipBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"test-attachment-export-{Guid.NewGuid()}.zip");
        await File.WriteAllBytesAsync(tempZipPath, exportedZipBytes);

        try
        {
            // Act 2: Import the game back
            output.WriteLine("Importing game with attachments...");
            await using var fileStream = File.OpenRead(tempZipPath);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", "game-import.zip");

            var importResponse = await adminClient.PostAsync("/api/Edit/Games/Import", content);
            importResponse.EnsureSuccessStatusCode();

            var importedGameId = await importResponse.Content.ReadFromJsonAsync<int>();
            output.WriteLine($"Imported game with new ID: {importedGameId}");

            // Assert: Verify attachment file names are preserved
            await ValidateImportedAttachmentFileNames(originalGame, importedGameId);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    /// <summary>
    /// Create a game with challenges that have attachments with specific file names
    /// </summary>
    private async Task<Game> CreateGameWithNamedAttachmentsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
        var blobRepo = scope.ServiceProvider.GetRequiredService<IBlobRepository>();

        // Create game
        var game = new Game
        {
            Title = $"Attachment Test Game {Guid.NewGuid().ToString("N")[..8]}",
            Summary = "Test game for attachment file names",
            Content = "Testing attachment file name preservation",
            Hidden = false,
            PracticeMode = true,
            AcceptWithoutReview = true,
            StartTimeUtc = DateTimeOffset.UtcNow.AddDays(-1),
            EndTimeUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await context.Games.AddAsync(game);
        await context.SaveChangesAsync();

        // Create challenge with attachment
        var challenge = new GameChallenge
        {
            GameId = game.Id,
            Title = "Attachment Challenge",
            Content = "Find the flag in the attachment",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            IsEnabled = true,
            OriginalScore = 100,
            MinScoreRate = 0.5,
            Difficulty = 1
        };

        var flag = new FlagContext
        {
            Flag = "flag{attachment_test}",
            Challenge = challenge
        };
        challenge.Flags.Add(flag);

        await challengeRepo.CreateChallenge(game, challenge, CancellationToken.None);

        // Create a test file and upload it as attachment
        var testFileName = "test_attachment.txt";
        var testFileContent = "This is a test attachment file content.";
        await using var testFileStream = new MemoryStream(Encoding.UTF8.GetBytes(testFileContent));
        var localFile = await blobRepo.CreateOrUpdateBlobFromStream(testFileName, testFileStream);

        // Update challenge with attachment
        await challengeRepo.UpdateAttachment(challenge,
            new AttachmentCreateModel
            {
                AttachmentType = FileType.Local,
                FileHash = localFile.Hash
            }, CancellationToken.None);

        // Reload game with attachments
        var reloadedGame = await context.Games
            .Include(g => g.Challenges)
            .ThenInclude(c => c.Attachment)
            .ThenInclude(a => a!.LocalFile)
            .FirstOrDefaultAsync(g => g.Id == game.Id);

        return reloadedGame!;
    }

    /// <summary>
    /// Validate that imported attachment file names match the original
    /// </summary>
    private async Task ValidateImportedAttachmentFileNames(Game originalGame, int importedGameId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var importedGame = await context.Games
            .Include(g => g.Challenges)
            .ThenInclude(c => c.Attachment)
            .ThenInclude(a => a!.LocalFile)
            .FirstOrDefaultAsync(g => g.Id == importedGameId);

        Assert.NotNull(importedGame);

        // Validate each challenge's attachment file name
        foreach (var originalChallenge in originalGame.Challenges.Where(c => c.Attachment is not null))
        {
            var importedChallenge = importedGame.Challenges.FirstOrDefault(c => c.Title == originalChallenge.Title);
            Assert.NotNull(importedChallenge);
            Assert.NotNull(importedChallenge.Attachment);
            Assert.NotNull(importedChallenge.Attachment.LocalFile);

            var originalFileName = originalChallenge.Attachment!.LocalFile!.Name;
            var importedFileName = importedChallenge.Attachment.LocalFile!.Name;

            Assert.Equal(originalFileName, importedFileName);
            output.WriteLine($"Attachment file name preserved: '{originalFileName}' ✓");
        }
    }

    /// <summary>
    /// Get a game with all challenges and attachments loaded
    /// </summary>
    private async Task<Game?> GetGameWithChallenges(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Games
            .Include(g => g.Challenges)
            .ThenInclude(c => c.Attachment)
            .ThenInclude(a => a!.LocalFile)
            .FirstOrDefaultAsync(g => g.Id == gameId);
    }

    #endregion
}
