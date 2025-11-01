using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
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
            using var fileStream = File.OpenRead(tempZipPath);
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

    #region Helper Methods

    /// <summary>
    /// Create a complex game with all features for comprehensive testing
    /// </summary>
    private async Task<Game> CreateComplexGameAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
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
            BloodBonus = new Utils.BloodBonus((50L << 20) + (30L << 10) + 10L) // First=50, Second=30, Third=10
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

        await challengeRepo.CreateChallenge(game, staticChallenge, default);

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

        await challengeRepo.CreateChallenge(game, staticContainerChallenge, default);

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
            FileName = "pwn" // This is the download filename for dynamic attachments
        };

        var dynamicFlag = new FlagContext
        {
            Flag = "flag{dyn4m1c_[GUID]}", // Template flag
            Challenge = dynamicChallenge
        };
        dynamicChallenge.Flags.Add(dynamicFlag);

        await challengeRepo.CreateChallenge(game, dynamicChallenge, default);

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

        await challengeRepo.CreateChallenge(game, multiFlag, default);

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
        var transferChallenges = new List<TransferChallenge>();
        foreach (var challengeEntry in challengeEntries)
        {
            await using var challengeStream = challengeEntry.Open();
            using var challengeReader = new StreamReader(challengeStream);
            var challengeJson = await challengeReader.ReadToEndAsync();
            var transferChallenge = TransferHelper.FromJson<TransferChallenge>(challengeJson);
            Assert.NotNull(transferChallenge);
            transferChallenges.Add(transferChallenge);
        }

        Assert.True(transferChallenges.Count >= 4);

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

        output.WriteLine($"Validated {transferChallenges.Count} challenges in exported ZIP");
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

    #endregion
}
