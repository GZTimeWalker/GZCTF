using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.UserModel;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Comprehensive integration tests for scoreboard calculations with divisions, permissions, and blood bonuses
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ScoreboardCalculationTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    /// <summary>
    /// Test scoreboard calculations with precise numerical score verification
    /// Verifies blood bonus calculation, dynamic scoring, and proper score accumulation
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldCalculate_PreciseScoresWithBloodBonuses()
    {
        // Setup: Create game with specific blood bonus configuration
        var game = await CreateGameWithBloodBonus(
            title: "Score Precision Test",
            packBloods(1.3f, 1.15f, 1.05f)
        );

        // Create 5 challenges with different base scores
        var challenge1 = await CreateChallenge(game.Id, "Easy Challenge", "flag{easy}", 500);
        var challenge2 = await CreateChallenge(game.Id, "Medium Challenge", "flag{medium}", 1000);
        var challenge3 = await CreateChallenge(game.Id, "Hard Challenge", "flag{hard}", 2000);

        // Create 5 teams
        var teams = await CreateMultipleTeams(5, "ScoreTest");

        // Teams solve challenges in order to get different blood bonuses
        // Team 1: Solves all 3 challenges (first blood on challenge1, second on challenge2)
        await JoinGameAndSolve(teams[0], game.Id, [
            (challenge1.Id, "flag{easy}"),
            (challenge2.Id, "flag{medium}"),
            (challenge3.Id, "flag{hard}")
        ]);

        // Team 2: Solves challenge1 and challenge2 (second blood on challenge1, first on challenge2)
        await JoinGameAndSolve(teams[1], game.Id, [
            (challenge1.Id, "flag{easy}"),
            (challenge2.Id, "flag{medium}")
        ]);

        // Team 3: Solves only challenge1 (third blood)
        await JoinGameAndSolve(teams[2], game.Id, [
            (challenge1.Id, "flag{easy}")
        ]);

        // Team 4: Solves challenge2 (third blood)
        await JoinGameAndSolve(teams[3], game.Id, [
            (challenge2.Id, "flag{medium}")
        ]);

        // Team 5: No solves (participates but doesn't solve)
        await JoinGame(teams[4], game.Id);

        // Force scoreboard recalculation
        await FlushScoreboardCache(game.Id);

        // Retrieve scoreboard after ensuring all solves are reflected
        var expectedTeamIds = teams.Select(t => t.team.Id).ToArray();
        var scoreboard = await GetScoreboard(
            teams[0].client,
            game.Id,
            expectedTeamIds,
            readiness: s =>
                s.GetTeam(teams[0].team.Id).SolvedChallenges.Count == 3 &&
                s.GetTeam(teams[1].team.Id).SolvedChallenges.Count == 2 &&
                s.GetTeam(teams[2].team.Id).SolvedChallenges.Count == 1);

        var scoreboardTeamIds = scoreboard.Teams.Select(t => t.Id).ToHashSet();
        Assert.True(scoreboardTeamIds.IsSupersetOf(teams.Take(4).Select(t => t.team.Id)),
            "All solving teams should appear on the scoreboard");

        // Expected scores (blood bonuses apply):
        // Team 1:
        //   - challenge1 (first blood): 500 * 1.3 = 650
        //   - challenge2 (first blood): 1000 * 1.3 = 1300
        //   - challenge3 (first blood): 2000 * 1.3 = 2600
        //   - Total: 650 + 1300 + 2600 = 4550
        //
        // Team 2:
        //   - challenge1 (second blood): 500 * 1.15 = 575
        //   - challenge2 (second blood): 1000 * 1.15 = 1150
        //   - Total: 575 + 1150 = 1725
        //
        // Team 3:
        //   - challenge1 (third blood): 500 * 1.05 = 525
        //   - Total: 525
        //
        // Team 4:
        //   - challenge2 (third blood): 1000 * 1.05 = 1050
        //   - Total: 1050
        //
        // Team 5: 0

        var team1Score = GetTeamScore(scoreboard, teams[0].team.Id);
        var team2Score = GetTeamScore(scoreboard, teams[1].team.Id);
        var team3Score = GetTeamScore(scoreboard, teams[2].team.Id);
        var team4Score = GetTeamScore(scoreboard, teams[3].team.Id);

        output.WriteLine($"Team 1 Score: {team1Score} (Expected: 4550)");
        output.WriteLine($"Team 2 Score: {team2Score} (Expected: 1725)");
        output.WriteLine($"Team 3 Score: {team3Score} (Expected: 525)");
        output.WriteLine($"Team 4 Score: {team4Score} (Expected: 1050)");

        if (scoreboardTeamIds.Contains(teams[4].team.Id))
        {
            var team5Score = GetTeamScore(scoreboard, teams[4].team.Id);
            output.WriteLine($"Team 5 Score: {team5Score} (Expected: 0)");
            Assert.Equal(0, team5Score);
        }

        Assert.Equal(4550, team1Score);
        Assert.Equal(1725, team2Score);
        Assert.Equal(525, team3Score);
        Assert.Equal(1050, team4Score);

        // Verify ranking order: Team1 > Team2 > Team4 > Team3 > Team5
        var rankedItems = scoreboard.Teams
            .OrderBy(team => team.Rank == 0 ? int.MaxValue : team.Rank)
            .ThenBy(team => team.Id)
            .ToList();

        Assert.Equal(teams[0].team.Id, rankedItems[0].Id);
        Assert.Equal(1, rankedItems[0].Rank);
        Assert.Equal(teams[1].team.Id, rankedItems[1].Id);
        Assert.Equal(2, rankedItems[1].Rank);
        Assert.Equal(teams[3].team.Id, rankedItems[2].Id);
        Assert.Equal(3, rankedItems[2].Rank);
        Assert.Equal(teams[2].team.Id, rankedItems[3].Id);
        Assert.Equal(4, rankedItems[3].Rank);

        if (scoreboardTeamIds.Contains(teams[4].team.Id))
        {
            var team5Entry = rankedItems.First(t => t.Id == teams[4].team.Id);
            Assert.Equal(5, team5Entry.Rank);
        }
    }

    /// <summary>
    /// Test division permissions for scoreboard visibility and scoring
    /// Verifies that division-specific permissions correctly control:
    /// - GetScore permission
    /// - GetBlood permission
    /// - RankOverall permission
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldRespect_DivisionPermissions()
    {
        var game = await CreateGameWithBloodBonus("Division Permission Test", packBloods(1.5f, 1.3f, 1.1f));
        var challenge1 = await CreateChallenge(game.Id, "Perm Challenge 1", "flag{perm1}", 1000);
        await CreateChallenge(game.Id, "Perm Challenge 2", "flag{perm2}", 1000);

        // Create divisions with different permissions
        var divisionA = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Division A - Full Permissions",
            InviteCode = "DIVA_FULL",
            DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview // Full permissions
        });

        var divisionB = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Division B - No Blood Bonus",
            InviteCode = "DIVB_NO_BLOOD",
            DefaultPermissions =
                GamePermission.All & ~GamePermission.GetBlood &
                ~GamePermission.RequireReview // Can score but no blood bonus
        });

        var divisionC = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Division C - No Overall Rank",
            InviteCode = "DIVC_NO_RANK",
            DefaultPermissions =
                GamePermission.All & ~GamePermission.RankOverall &
                ~GamePermission.RequireReview // Can score but not ranked overall
        });

        var divisionD = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Division D - No Score",
            InviteCode = "DIVD_NO_SCORE",
            DefaultPermissions =
                GamePermission.All & ~GamePermission.GetScore &
                ~GamePermission.RequireReview // Can participate but no score
        });

        // Create teams in each division
        var teamA = await CreateTeamInDivision("TeamA", game.Id, divisionA.Id, "DIVA_FULL");
        var teamB = await CreateTeamInDivision("TeamB", game.Id, divisionB.Id, "DIVB_NO_BLOOD");
        var teamC = await CreateTeamInDivision("TeamC", game.Id, divisionC.Id, "DIVC_NO_RANK");
        var teamD = await CreateTeamInDivision("TeamD", game.Id, divisionD.Id, "DIVD_NO_SCORE");

        // All teams solve challenge1 first
        await SubmitFlag(teamA.client, game.Id, challenge1.Id, "flag{perm1}");
        await Task.Delay(100); // Small delay to ensure different submission times
        await SubmitFlag(teamB.client, game.Id, challenge1.Id, "flag{perm1}");
        await Task.Delay(100);
        await SubmitFlag(teamC.client, game.Id, challenge1.Id, "flag{perm1}");
        await Task.Delay(100);
        await SubmitFlag(teamD.client, game.Id, challenge1.Id, "flag{perm1}");

        // Force scoreboard recalculation
        await FlushScoreboardCache(game.Id);

        // Retrieve scoreboard with expected teams and solves reflected
        var divisionTeamIds = new[] { teamA.team.Id, teamB.team.Id, teamC.team.Id, teamD.team.Id };
        var scoreboard = await GetScoreboard(
            teamA.client,
            game.Id,
            divisionTeamIds,
            readiness: s => s.GetTeam(teamA.team.Id).SolvedChallenges.Count == 1);

        output.WriteLine("=== Scoreboard Results ===");
        foreach (var item in scoreboard.Teams.OrderBy(i => i.Rank == 0 ? int.MaxValue : i.Rank))
        {
            output.WriteLine($"Team: {item.Name}, Division: {item.DivisionId}, Score: {item.Score}, " +
                             $"Rank: {item.Rank}, DivisionRank: {item.DivisionRank}");
        }

        // Verify scores:
        // Team A (Division A - Full): First blood = 1000 * 1.5 = 1500
        // Team B (Division B - No Blood): Normal score = 1000 (no blood bonus)
        // Team C (Division C - No Overall Rank): Can get blood (second blood) = 1000 * 1.3 = 1300
        // Team D (Division D - No Score): No score = 0

        var scoreA = GetTeamScore(scoreboard, teamA.team.Id);
        var scoreB = GetTeamScore(scoreboard, teamB.team.Id);
        var scoreC = GetTeamScore(scoreboard, teamC.team.Id);
        var scoreD = GetTeamScore(scoreboard, teamD.team.Id);

        output.WriteLine($"\nTeam A Score: {scoreA} (Expected: 1500 - First Blood)");
        output.WriteLine($"Team B Score: {scoreB} (Expected: 1000 - No Blood Bonus)");
        output.WriteLine($"Team C Score: {scoreC} (Expected: 1300 - Second Blood)");
        output.WriteLine($"Team D Score: {scoreD} (Expected: 0 - No Score Permission)");

        Assert.Equal(1500, scoreA);
        Assert.Equal(1000, scoreB);
        Assert.Equal(1300, scoreC);
        Assert.Equal(0, scoreD);

        // Verify overall ranking (Team C should not have overall rank)
        var itemA = scoreboard.GetTeam(teamA.team.Id);
        var itemB = scoreboard.GetTeam(teamB.team.Id);
        var itemC = scoreboard.GetTeam(teamC.team.Id);
        var itemD = scoreboard.GetTeam(teamD.team.Id);

        Assert.Equal(1, itemA.Rank); // Team A is first
        Assert.Equal(2, itemB.Rank); // Team B is second
        Assert.Equal(0, itemC.Rank); // Team C has no overall rank (no RankOverall permission)
        Assert.Equal(3, itemD.Rank); // Team D is third (even with 0 score, it gets ranked)

        // Verify division rankings
        Assert.Equal(1, itemA.DivisionRank); // First in Division A
        Assert.Equal(1, itemB.DivisionRank); // First in Division B
        Assert.Equal(1, itemC.DivisionRank); // First in Division C
        Assert.Equal(1, itemD.DivisionRank); // First in Division D
    }

    /// <summary>
    /// Test division-specific challenge permissions
    /// Verifies that challenge-specific permissions override default division permissions
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldRespect_ChallengeSpecificPermissions()
    {
        var game = await CreateGameWithBloodBonus("Challenge Permission Test", packBloods(1.5f, 1.3f, 1.1f));
        var challenge1 = await CreateChallenge(game.Id, "Open Challenge", "flag{open}", 1000);
        var challenge2 = await CreateChallenge(game.Id, "Restricted Challenge", "flag{restricted}", 2000);

        // Create division with:
        // - Default: All permissions except RequireReview (auto-accept)
        // - Challenge2: No score permission (view and submit only)
        var division = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Mixed Permission Division",
            InviteCode = "MIXED_PERM",
            DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview,
            ChallengeConfigs =
            [
                new()
                {
                    ChallengeId = challenge2.Id,
                    Permissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags // No GetScore or GetBlood
                }
            ]
        });

        var team = await CreateTeamInDivision("MixedTeam", game.Id, division.Id, "MIXED_PERM");

        // Solve both challenges
        await SubmitFlag(team.client, game.Id, challenge1.Id, "flag{open}");
        await SubmitFlag(team.client, game.Id, challenge2.Id, "flag{restricted}");

        // Force scoreboard recalculation
        await FlushScoreboardCache(game.Id);

        var scoreboard = await GetScoreboard(
            team.client,
            game.Id,
            expectedTeamIds: [team.team.Id],
            readiness: s => s.GetTeam(team.team.Id).SolvedChallenges.Any(c => c.Id == challenge1.Id));

        // Team should get:
        // - challenge1: 1000 * 1.5 = 1500 (first blood)
        // - challenge2: 0 (no score permission)
        // Total: 1500

        var item = scoreboard.GetTeam(team.team.Id);

        var score = item.Score;
        output.WriteLine($"Team Score: {score} (Expected: 1500)");
        foreach (var solved in item.SolvedChallenges)
        {
            output.WriteLine($"Solved challenge {solved.Id} with score {solved.Score} ({solved.Type})");
        }

        Assert.Equal(1500, score);

        // Verify solved challenges count
        Assert.Contains(item.SolvedChallenges, c => c.Id == challenge1.Id);
        Assert.DoesNotContain(item.SolvedChallenges, c => c.Id == challenge2.Id && c.Score > 0);
    }

    /// <summary>
    /// Test dynamic scoring calculation
    /// Verifies that challenge scores decrease as more teams solve them
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldCalculate_DynamicScoring()
    {
        var game = await CreateGameWithBloodBonus("Dynamic Scoring Test",
            packBloods(1.0f, 1.0f, 1.0f)); // No blood bonus

        // Create challenge with dynamic scoring
        var challenge = await CreateDynamicChallenge(
            game.Id,
            "Dynamic Challenge",
            "flag{dynamic}",
            originalScore: 1000,
            minScoreRate: 0.2, // Minimum 20% of original score
            difficulty: 10 // Higher difficulty = faster decay
        );

        // Create 10 teams
        var teams = await CreateMultipleTeams(10, "DynTeam");

        // Teams solve sequentially
        ScoreboardSnapshot? previousScoreboard = null;

        for (int i = 0; i < 10; i++)
        {
            await JoinGameAndSolve(teams[i], game.Id, [(challenge.Id, "flag{dynamic}")]);

            // Force recalculation after each solve to see score changes
            await FlushScoreboardCache(game.Id);

            var expectedIds = teams.Take(i + 1).Select(t => t.team.Id).ToArray();
            var scoreboard = await GetScoreboard(
                teams[i].client,
                game.Id,
                expectedIds,
                readiness: s =>
                {
                    var challengeInfo = s.GetChallenge(challenge.Id);
                    var teamInfo = s.GetTeam(teams[i].team.Id);
                    return challengeInfo.SolvedCount >= i + 1 && teamInfo.SolvedChallenges.Count >= 1;
                });
            var currentScore = scoreboard.GetChallenge(challenge.Id).Score;

            output.WriteLine($"After {i + 1} teams solved: Score = {currentScore}");

            // Verify score is decreasing (or staying at minimum)
            if (previousScoreboard is not null)
            {
                var prevScore = previousScoreboard.GetChallenge(challenge.Id).Score;

                Assert.True(currentScore <= prevScore,
                    $"Score should decrease or stay the same: prev={prevScore}, current={currentScore}");
            }

            // Verify minimum score is respected
            Assert.True(currentScore >= 200, // 1000 * 0.2 = 200
                $"Score should not go below minimum: {currentScore} < 200");

            previousScoreboard = scoreboard;
        }
    }

    /// <summary>
    /// Test scoreboard with multiple divisions and complex permission combinations
    /// Verifies comprehensive division interaction scenarios
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldHandle_ComplexMultiDivisionScenarios()
    {
        var game = await CreateGameWithBloodBonus("Multi-Division Test", packBloods(2.0f, 1.5f, 1.25f));

        var challenge1 = await CreateChallenge(game.Id, "Public Challenge", "flag{public}", 500);
        var challenge2 = await CreateChallenge(game.Id, "Exclusive Challenge", "flag{exclusive}", 1000);
        var challenge3 = await CreateChallenge(game.Id, "Mixed Challenge", "flag{mixed}", 1500);

        // Division 1: Professional - Full access, can rank overall
        var divPro = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "Professional",
                InviteCode = "PRO2025",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });

        // Division 2: Student - Full access, can rank overall
        var divStudent = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "Student",
                InviteCode = "STU2025",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });

        // Division 3: Unofficial - Can play but no overall ranking, no blood bonuses
        var divUnofficial = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "Unofficial",
                InviteCode = "UNOFF2025",
                DefaultPermissions = GamePermission.All & ~GamePermission.RankOverall & ~GamePermission.GetBlood &
                                     ~GamePermission.RequireReview
            });

        // Division 4: Observer - Can only see challenge2 and challenge3, no scoring
        var divObserver = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Observer",
            InviteCode = "OBS2025",
            DefaultPermissions = GamePermission.JoinGame, // No scoring by default
            ChallengeConfigs =
            [
                new() { ChallengeId = challenge2.Id, Permissions = GamePermission.ViewChallenge },
                new() { ChallengeId = challenge3.Id, Permissions = GamePermission.ViewChallenge }
            ]
        });

        // Create 2 teams per division
        var teamPro1 = await CreateTeamInDivision("Pro1", game.Id, divPro.Id, "PRO2025");
        var teamPro2 = await CreateTeamInDivision("Pro2", game.Id, divPro.Id, "PRO2025");
        var teamStu1 = await CreateTeamInDivision("Stu1", game.Id, divStudent.Id, "STU2025");
        var teamStu2 = await CreateTeamInDivision("Stu2", game.Id, divStudent.Id, "STU2025");
        var teamUnoff1 = await CreateTeamInDivision("Unoff1", game.Id, divUnofficial.Id, "UNOFF2025");
        var teamObs1 = await CreateTeamInDivision("Obs1", game.Id, divObserver.Id, "OBS2025");

        // Solve scenarios:
        // Pro1: Solves all 3 (first blood on all)
        await SubmitFlag(teamPro1.client, game.Id, challenge1.Id, "flag{public}");
        await SubmitFlag(teamPro1.client, game.Id, challenge2.Id, "flag{exclusive}");
        await SubmitFlag(teamPro1.client, game.Id, challenge3.Id, "flag{mixed}");
        await Task.Delay(100);

        // Stu1: Solves challenge1 and challenge2 (second blood)
        await SubmitFlag(teamStu1.client, game.Id, challenge1.Id, "flag{public}");
        await SubmitFlag(teamStu1.client, game.Id, challenge2.Id, "flag{exclusive}");
        await Task.Delay(100);

        // Pro2: Solves challenge1 (third blood)
        await SubmitFlag(teamPro2.client, game.Id, challenge1.Id, "flag{public}");
        await Task.Delay(100);

        // Stu2: Solves challenge3 (second blood)
        await SubmitFlag(teamStu2.client, game.Id, challenge3.Id, "flag{mixed}");
        await Task.Delay(100);

        // Unoff1: Solves all 3 (should get normal scores, no blood bonus, no overall rank)
        await SubmitFlag(teamUnoff1.client, game.Id, challenge1.Id, "flag{public}");
        await SubmitFlag(teamUnoff1.client, game.Id, challenge2.Id, "flag{exclusive}");
        await SubmitFlag(teamUnoff1.client, game.Id, challenge3.Id, "flag{mixed}");

        // Obs1: Tries to solve (should not get any score)
        var obsExclusive = await SubmitFlag(
            teamObs1.client,
            game.Id,
            challenge2.Id,
            "flag{exclusive}",
            ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, obsExclusive.StatusCode);

        var obsMixed = await SubmitFlag(
            teamObs1.client,
            game.Id,
            challenge3.Id,
            "flag{mixed}",
            ensureSuccess: false);
        Assert.Equal(HttpStatusCode.BadRequest, obsMixed.StatusCode);

        // Force scoreboard recalculation
        await FlushScoreboardCache(game.Id);

        var multiDivisionTeamIds = new[]
        {
            teamPro1.team.Id, teamPro2.team.Id, teamStu1.team.Id, teamStu2.team.Id, teamUnoff1.team.Id,
            teamObs1.team.Id
        };

        var scoreboard = await GetScoreboard(
            teamPro1.client,
            game.Id,
            multiDivisionTeamIds,
            readiness: s =>
                s.GetTeam(teamPro1.team.Id).SolvedChallenges.Count == 3 &&
                s.GetTeam(teamStu1.team.Id).SolvedChallenges.Count == 2 &&
                s.GetTeam(teamUnoff1.team.Id).SolvedChallenges.Count == 3);

        // Expected scores:
        // Pro1: 500*2.0 + 1000*2.0 + 1500*2.0 = 1000 + 2000 + 3000 = 6000
        // Stu1: 500*1.5 + 1000*1.5 = 750 + 1500 = 2250
        // Pro2: 500*1.25 = 625
        // Stu2: 1500*1.5 = 2250
        // Unoff1: 500 + 1000 + 1500 = 3000 (no blood bonus)
        // Obs1: 0 (no score permission)

        var scorePro1 = GetTeamScore(scoreboard, teamPro1.team.Id);
        var scoreStu1 = GetTeamScore(scoreboard, teamStu1.team.Id);
        var scorePro2 = GetTeamScore(scoreboard, teamPro2.team.Id);
        var scoreStu2 = GetTeamScore(scoreboard, teamStu2.team.Id);
        var scoreUnoff1 = GetTeamScore(scoreboard, teamUnoff1.team.Id);
        var scoreObs1 = GetTeamScore(scoreboard, teamObs1.team.Id);

        output.WriteLine("=== Final Scores ===");
        output.WriteLine($"Pro1: {scorePro1} (Expected: 6000)");
        output.WriteLine($"Stu1: {scoreStu1} (Expected: 2250)");
        output.WriteLine($"Pro2: {scorePro2} (Expected: 625)");
        output.WriteLine($"Stu2: {scoreStu2} (Expected: 2250)");
        output.WriteLine($"Unoff1: {scoreUnoff1} (Expected: 3000)");
        output.WriteLine($"Obs1: {scoreObs1} (Expected: 0)");

        Assert.Equal(6000, scorePro1);
        Assert.Equal(2250, scoreStu1);
        Assert.Equal(625, scorePro2);
        Assert.Equal(2250, scoreStu2);
        Assert.Equal(3000, scoreUnoff1);
        Assert.Equal(0, scoreObs1);

        // Verify overall rankings (Unoff1 and Obs1 should not be ranked overall)
        var itemPro1 = scoreboard.GetTeam(teamPro1.team.Id);
        var itemStu1 = scoreboard.GetTeam(teamStu1.team.Id);
        var itemStu2 = scoreboard.GetTeam(teamStu2.team.Id);
        var itemPro2 = scoreboard.GetTeam(teamPro2.team.Id);
        var itemUnoff1 = scoreboard.GetTeam(teamUnoff1.team.Id);
        var itemObs1 = scoreboard.GetTeam(teamObs1.team.Id);

        Assert.Equal(1, itemPro1.Rank); // First overall
        Assert.True(itemStu1.Rank == 2 || itemStu2.Rank == 2); // Second or third (tied scores)
        Assert.True(itemStu1.Rank == 3 || itemStu2.Rank == 3); // Second or third (tied scores)
        Assert.Equal(4, itemPro2.Rank); // Fourth overall
        Assert.Equal(0, itemUnoff1.Rank); // No overall rank (unofficial)
        Assert.Equal(0, itemObs1.Rank); // Observer division is excluded from overall ranking

        // Verify division rankings
        Assert.Equal(1, itemPro1.DivisionRank); // First in Professional
        Assert.Equal(2, itemPro2.DivisionRank); // Second in Professional
        Assert.True(itemStu1.DivisionRank == 1 || itemStu2.DivisionRank == 1); // First in Student (tied)
        Assert.True(itemStu1.DivisionRank == 1 || itemStu2.DivisionRank == 1); // Tied in Student
        Assert.Equal(1, itemUnoff1.DivisionRank); // First in Unofficial
        Assert.Equal(1, itemObs1.DivisionRank); // First in Observer

        // Verify challenge blood bonuses are correctly assigned
        var challenge1Info = scoreboard.GetChallenge(challenge1.Id);

        Assert.Equal(3, challenge1Info.Bloods.Count); // Should have 3 bloods
        Assert.Equal(teamPro1.team.Id, challenge1Info.Bloods[0].Id); // First blood
        Assert.Equal(teamStu1.team.Id, challenge1Info.Bloods[1].Id); // Second blood
        Assert.Equal(teamPro2.team.Id, challenge1Info.Bloods[2].Id); // Third blood
    }

    /// <summary>
    /// Test AffectDynamicScore permission - separates scoring from dynamic score calculation
    /// Verifies that teams can score without affecting challenge dynamic scoring
    /// Use case: External participants score normally but don't affect internal competition's dynamic scoring
    /// </summary>
    [Fact]
    public async Task Scoreboard_ShouldRespect_AffectDynamicScorePermission()
    {
        var game = await CreateGameWithBloodBonus("AffectDynamicScore Test", packBloods(1.0f, 1.0f, 1.0f));

        // Create a dynamic scoring challenge
        var challenge = await CreateDynamicChallenge(
            game.Id,
            "Dynamic Challenge",
            "flag{dynamic}",
            originalScore: 1000,
            minScoreRate: 0.2, // Min 20% of original
            difficulty: 10
        );

        // Division 1: Internal - Full permissions including AffectDynamicScore
        var divInternal = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Internal",
            InviteCode = "INTERNAL",
            DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview // Includes AffectDynamicScore
        });

        // Division 2: External - Can score but doesn't affect dynamic scoring
        var divExternal = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "External",
                InviteCode = "EXTERNAL",
                DefaultPermissions = GamePermission.All & ~GamePermission.AffectDynamicScore &
                                     ~GamePermission.RequireReview
            });

        // Division 3: Observer - Cannot score and doesn't affect dynamic scoring
        var divObserver = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = "Observer",
            InviteCode = "OBSERVER",
            DefaultPermissions =
                GamePermission.JoinGame | GamePermission.ViewChallenge |
                GamePermission.SubmitFlags // No GetScore or AffectDynamicScore
        });

        // Create teams in each division
        var teamInternal1 = await CreateTeamInDivision("Internal1", game.Id, divInternal.Id, "INTERNAL");
        var teamInternal2 = await CreateTeamInDivision("Internal2", game.Id, divInternal.Id, "INTERNAL");
        var teamExternal1 = await CreateTeamInDivision("External1", game.Id, divExternal.Id, "EXTERNAL");
        var teamExternal2 = await CreateTeamInDivision("External2", game.Id, divExternal.Id, "EXTERNAL");
        var teamObserver = await CreateTeamInDivision("Observer", game.Id, divObserver.Id, "OBSERVER");

        // Internal teams solve (affects dynamic score)
        await SubmitFlag(teamInternal1.client, game.Id, challenge.Id, "flag{dynamic}");
        await SubmitFlag(teamInternal2.client, game.Id, challenge.Id, "flag{dynamic}");

        // Get scoreboard after 2 internal solves
        await FlushScoreboardCache(game.Id);
        var scoreboard1 = await GetScoreboard(
            teamInternal1.client,
            game.Id,
            expectedTeamIds: [teamInternal1.team.Id, teamInternal2.team.Id],
            readiness: s => s.GetChallenge(challenge.Id).SolvedCount >= 2
        );

        var scoreAfter2Internal = scoreboard1.GetChallenge(challenge.Id).Score;
        output.WriteLine($"Score after 2 internal solves: {scoreAfter2Internal}");

        // External teams solve (should get points but not affect dynamic score)
        await SubmitFlag(teamExternal1.client, game.Id, challenge.Id, "flag{dynamic}");
        await SubmitFlag(teamExternal2.client, game.Id, challenge.Id, "flag{dynamic}");

        // Observer solves (no points, no affect)
        await SubmitFlag(teamObserver.client, game.Id, challenge.Id, "flag{dynamic}");

        // Get final scoreboard
        await FlushScoreboardCache(game.Id);
        var scoreboard2 = await GetScoreboard(
            teamInternal1.client,
            game.Id,
            expectedTeamIds:
            [teamInternal1.team.Id, teamInternal2.team.Id, teamExternal1.team.Id, teamExternal2.team.Id],
            readiness: s =>
            {
                var internal1Solved = s.TeamsById.TryGetValue(teamInternal1.team.Id, out var t1) &&
                                      t1.SolvedChallenges.Count >= 1;
                var internal2Solved = s.TeamsById.TryGetValue(teamInternal2.team.Id, out var t2) &&
                                      t2.SolvedChallenges.Count >= 1;
                var external1Solved = s.TeamsById.TryGetValue(teamExternal1.team.Id, out var t3) &&
                                      t3.SolvedChallenges.Count >= 1;
                var external2Solved = s.TeamsById.TryGetValue(teamExternal2.team.Id, out var t4) &&
                                      t4.SolvedChallenges.Count >= 1;
                return internal1Solved && internal2Solved && external1Solved && external2Solved;
            }
        );

        var scoreAfterExternal = scoreboard2.GetChallenge(challenge.Id).Score;
        var solvedCount = scoreboard2.GetChallenge(challenge.Id).SolvedCount;

        output.WriteLine($"Score after external solves: {scoreAfterExternal}");
        output.WriteLine($"Solved count (should be 2, only internal): {solvedCount}");

        // Verify: Challenge score should be same as after 2 internal solves
        // because external solves don't affect dynamic scoring
        Assert.Equal(scoreAfter2Internal, scoreAfterExternal);
        Assert.Equal(2, solvedCount); // Only internal teams count

        // Verify scores of teams
        var scoreInternal1 = GetTeamScore(scoreboard2, teamInternal1.team.Id);
        var scoreInternal2 = GetTeamScore(scoreboard2, teamInternal2.team.Id);
        var scoreExternal1 = GetTeamScore(scoreboard2, teamExternal1.team.Id);
        var scoreExternal2 = GetTeamScore(scoreboard2, teamExternal2.team.Id);

        output.WriteLine($"Internal1 score: {scoreInternal1}");
        output.WriteLine($"Internal2 score: {scoreInternal2}");
        output.WriteLine($"External1 score: {scoreExternal1}");
        output.WriteLine($"External2 score: {scoreExternal2}");

        // All teams that can score should have the same score
        // (because they all solved at the same challenge score value)
        Assert.True(scoreInternal1 > 0, "Internal team should have score");
        Assert.True(scoreInternal2 > 0, "Internal team should have score");
        Assert.True(scoreExternal1 > 0, "External team should have score");
        Assert.True(scoreExternal2 > 0, "External team should have score");
        Assert.Equal(scoreInternal1, scoreExternal1); // Same score value

        // Observer should have no score
        if (scoreboard2.TeamsById.ContainsKey(teamObserver.team.Id))
        {
            var scoreObserver = GetTeamScore(scoreboard2, teamObserver.team.Id);
            Assert.Equal(0, scoreObserver);
        }
    }


    /// <summary>
    /// Test ScoreboardSheet download
    /// </summary>
    [Fact]
    public async Task ScoreboardSheet_ShouldContainTeamRankingsAndScores()
    {
        // Setup: Create monitor user
        var monitorPassword = "Monitor@Sheet123";
        var monitorUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), monitorPassword, role: Role.Monitor);

        // Create game with specific scoring setup
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Game For Excel Export");
        var challenge1 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Challenge 1", "flag{one}", originalScore: 1000);
        var challenge2 = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Challenge 2", "flag{two}", originalScore: 500);

        // Create two divisions:
        // 1. Regular: Full permissions with RankOverall
        var divRegular = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "Regular Division",
                InviteCode = "REGULAR",
                DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview
            });

        // 2. Special: No RankOverall permission
        var divSpecial = await CreateDivision(game.Id,
            new DivisionCreateModel
            {
                Name = "Special Division",
                InviteCode = "SPECIAL",
                DefaultPermissions =
                    GamePermission.All & ~GamePermission.RankOverall & ~GamePermission.RequireReview
            });

        // Create three teams:
        // Team 1 & 3 in Regular division, Team 2 in Special division
        var team1 = await CreateTeamInDivision("Excel Team 1", game.Id, divRegular.Id, "REGULAR");
        var team2 = await CreateTeamInDivision("Excel Team 2", game.Id, divSpecial.Id, "SPECIAL");
        var team3 = await CreateTeamInDivision("Excel Team 3", game.Id, divRegular.Id, "REGULAR");

        // Solve challenges:
        // Team 1: Solves both challenges (highest score = 1500)
        await SubmitFlag(team1.client, game.Id, challenge1.Id, "flag{one}");
        await SubmitFlag(team1.client, game.Id, challenge2.Id, "flag{two}");
        await Task.Delay(100);

        // Team 2: Solves only challenge 1 (score = 1000, but no overall rank)
        await SubmitFlag(team2.client, game.Id, challenge1.Id, "flag{one}");
        await Task.Delay(100);

        // Team 3: Solves nothing (score = 0)
        // (just join the game)

        // Flush scoreboard cache to ensure latest data
        await FlushScoreboardCache(game.Id);

        // Download scoreboard as monitor
        using var monitorClient = factory.CreateClient();
        await monitorClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = monitorUser.UserName, Password = monitorPassword });

        // Test: Non-existent game returns 404
        var notFoundResponse = await monitorClient.GetAsync("/api/Game/99999/ScoreboardSheet");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

        // Test: Non-started game returns BadRequest
        var futureStart = DateTimeOffset.UtcNow.AddHours(1);
        var futureGame = await TestDataSeeder.CreateGameAsync(factory.Services, "Future Game",
            start: futureStart);
        var futureResponse = await monitorClient.GetAsync($"/api/Game/{futureGame.Id}/ScoreboardSheet");
        Assert.Equal(HttpStatusCode.BadRequest, futureResponse.StatusCode);

        // Test: Non-monitor user returns Forbidden
        var normalUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Pass@123", role: Role.User);
        using var userClient = factory.CreateClient();
        await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = normalUser.UserName, Password = "Pass@123" });
        var forbiddenResponse = await userClient.GetAsync($"/api/Game/{game.Id}/ScoreboardSheet");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // Main test: Download and verify Excel content
        var scoreboardResponse = await monitorClient.GetAsync($"/api/Game/{game.Id}/ScoreboardSheet");
        scoreboardResponse.EnsureSuccessStatusCode();

        // Verify Excel file format
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            scoreboardResponse.Content.Headers.ContentType?.MediaType);

        // Parse and validate Excel structure
        await using var excelStream = await scoreboardResponse.Content.ReadAsStreamAsync();
        Assert.True(excelStream.Length > 0, "Excel file should not be empty");

        using var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(excelStream);
        var sheet = workbook.GetSheetAt(0);

        // Excel structure (from ExcelHelper):
        // Column 0: Rank (or "-" for unranked)
        // Column 1: Team Name
        // Column 2: Division (if divisions exist)
        // Column 3+: Challenge scores

        Assert.True(sheet.PhysicalNumberOfRows >= 3,
            $"Excel should contain at least 1 header row + 3 teams, got {sheet.PhysicalNumberOfRows} rows");

        // Extract team data from Excel rows
        var teamDataMap = new Dictionary<string, (int rank, int totalScore, int[] challengeScores)>();

        // Row 0 is header, find challenge column indices
        var headerRow = sheet.GetRow(0);

        // Find the index where challenge columns start and where total score is
        // Based on ExcelHelper: Rank, Team, [Division], Captain, Members, RealName, Email, StdNumber, Phone, SolvedCount, ScoringTime, TotalScore, [Challenges...]
        int totalScoreColumnIndex = -1;
        int challengeStartColumnIndex = -1;

        // Count header cells to find structure
        for (int i = 0; i < headerRow.LastCellNum; i++)
        {
            var headerCell = headerRow.GetCell(i);
            var headerValue = GetCellStringValue(headerCell);

            // The header contains "Total Score" or similar
            if (headerValue.Contains("Score") && i > 5) // Likely the summary score column
                totalScoreColumnIndex = i;
        }

        // If we found total score, challenges start after it
        if (totalScoreColumnIndex >= 0)
            challengeStartColumnIndex = totalScoreColumnIndex + 1;

        // Process data rows (starting from row 1)
        for (int row = 1; row < sheet.PhysicalNumberOfRows; row++)
        {
            var dataRow = sheet.GetRow(row);
            if (dataRow == null)
                continue;

            // Column 0: Rank (or "-" for unranked)
            var rankCell = dataRow.GetCell(0);
            string rankValue = GetCellStringValue(rankCell);

            // Column 1: Team Name
            var teamNameCell = dataRow.GetCell(1);
            string teamName = GetCellStringValue(teamNameCell);

            // Get total score from the expected column (usually around column 10-12)
            int totalScore = 0;
            if (totalScoreColumnIndex >= 0 && totalScoreColumnIndex < dataRow.LastCellNum)
            {
                var scoreCell = dataRow.GetCell(totalScoreColumnIndex);
                if (scoreCell?.CellType == CellType.Numeric)
                    totalScore = (int)scoreCell.NumericCellValue;
            }

            // Get challenge-specific scores
            var challengeScores = new List<int>();
            if (challengeStartColumnIndex >= 0)
            {
                for (int col = challengeStartColumnIndex; col < dataRow.LastCellNum; col++)
                {
                    var cell = dataRow.GetCell(col);
                    if (cell is { CellType: CellType.Numeric })
                        challengeScores.Add((int)cell.NumericCellValue);
                }
            }

            teamDataMap[teamName] = (ParseRank(rankValue), totalScore, challengeScores.ToArray());
        }

        // Verify team data
        output.WriteLine("=== Excel Data Verification ===");

        // Debug: Print all extracted data
        foreach (var kvp in teamDataMap)
            output.WriteLine(
                $"  {kvp.Key}: Rank={kvp.Value.rank}, TotalScore={kvp.Value.totalScore}, Challenges=[{string.Join(",", kvp.Value.challengeScores)}]");

        // Debug: Print raw Excel content for inspection
        output.WriteLine("\n=== Raw Excel Row Data ===");
        for (int row = 0; row < Math.Min(5, sheet.PhysicalNumberOfRows); row++)
        {
            var dataRow = sheet.GetRow(row);
            if (dataRow == null)
                continue;

            var rowData = new List<string>();
            for (int col = 0; col < dataRow.LastCellNum; col++)
            {
                rowData.Add($"[{col}]={GetCellStringValue(dataRow.GetCell(col))}");
            }

            output.WriteLine($"Row {row}: {string.Join(", ", rowData)}");
        }

        // Team 1: Should have numeric rank (1), in Regular division (RankOverall=true)
        Assert.True(teamDataMap.TryGetValue("Excel Team 1", out var team1Data), "Team 1 should be in Excel");
        output.WriteLine(
            $"\nTeam 1 Verification: Rank={team1Data.rank}, TotalScore={team1Data.totalScore}, Challenges=[{string.Join(",", team1Data.challengeScores)}]");

        // Verify Team 1 has a numeric rank (not 0, which means "-")
        Assert.NotEqual(0, team1Data.rank);
        Assert.Equal(1, team1Data.rank);
        output.WriteLine($"  ✓ Team 1 has numeric rank {team1Data.rank} (RankOverall permission enabled)");

        // Verify Team 1 solved both challenges
        Assert.True(team1Data.challengeScores.Length >= 2, "Team 1 should have at least 2 challenge scores");
        Assert.True(team1Data.challengeScores[0] > 0, "Team 1 should have score for Challenge 1");
        Assert.True(team1Data.challengeScores[1] > 0, "Team 1 should have score for Challenge 2");
        output.WriteLine(
            $"  ✓ Team 1 solved both challenges with scores {team1Data.challengeScores[0]} and {team1Data.challengeScores[1]}");

        // Total score should be sum of challenge scores
        int team1ExpectedMin = team1Data.challengeScores[0] + team1Data.challengeScores[1];
        Assert.True(team1Data.totalScore == team1ExpectedMin,
            $"Team 1 total score {team1Data.totalScore} should be at least sum of challenges {team1ExpectedMin}");
        output.WriteLine($"  ✓ Team 1 total score {team1Data.totalScore} >= {team1ExpectedMin}");

        // Team 2: Should have rank 0 (displayed as "-"), in Special division (RankOverall=false)
        Assert.True(teamDataMap.TryGetValue("Excel Team 2", out var team2Data), "Team 2 should be in Excel");
        output.WriteLine(
            $"\nTeam 2 Verification: Rank={team2Data.rank}, TotalScore={team2Data.totalScore}, Challenges=[{string.Join(",", team2Data.challengeScores)}]");

        // Verify Team 2 has NO overall rank (rank=0 means "-")
        Assert.Equal(0, team2Data.rank);
        output.WriteLine("  ✓ Team 2 has no overall rank (displayed as \"-\") due to RankOverall permission disabled");

        // Verify Team 2 solved only Challenge 1
        Assert.True(team2Data.challengeScores.Length >= 2, "Team 2 should have scores for both challenge columns");
        Assert.True(team2Data.challengeScores[0] > 0, "Team 2 should have score for Challenge 1");
        Assert.Equal(0, team2Data.challengeScores[1]);
        output.WriteLine(
            $"  ✓ Team 2 solved only Challenge 1 with score {team2Data.challengeScores[0]}, Challenge 2 score is 0");

        // Verify Team 2's total score matches Challenge 1 score (no overall rank but still has division-specific scoring)
        Assert.True(team2Data.totalScore >= team2Data.challengeScores[0],
            $"Team 2 total score {team2Data.totalScore} should include Challenge 1 score {team2Data.challengeScores[0]}");
        output.WriteLine($"  ✓ Team 2 total score {team2Data.totalScore} includes Challenge 1 score");

        // Team 3: Should have numeric rank (2), in Regular division (RankOverall=true), no challenges solved
        Assert.True(teamDataMap.TryGetValue("Excel Team 3", out var team3Data), "Team 3 should be in Excel");
        output.WriteLine(
            $"\nTeam 3 Verification: Rank={team3Data.rank}, TotalScore={team3Data.totalScore}, Challenges=[{string.Join(",", team3Data.challengeScores)}]");

        // Verify Team 3 has numeric rank (ranked 2nd in Regular division)
        Assert.NotEqual(0, team3Data.rank);
        Assert.Equal(2, team3Data.rank);
        output.WriteLine($"  ✓ Team 3 has numeric rank {team3Data.rank} (RankOverall permission enabled)");

        // Verify Team 3 solved no challenges
        Assert.True(team3Data.challengeScores.Length >= 2, "Team 3 should have 2 challenge columns");
        Assert.Equal(0, team3Data.challengeScores[0]);
        Assert.Equal(0, team3Data.challengeScores[1]);
        output.WriteLine("  ✓ Team 3 solved no challenges (all scores are 0)");

        // Verify Team 3's total score is 0
        Assert.Equal(0, team3Data.totalScore);
        output.WriteLine("  ✓ Team 3 total score is 0");

        // Test: Download all submissions
        output.WriteLine("\n=== Testing Submission Sheet Download ===");

        // Test: Non-existent game returns 404
        var submissionNotFoundResponse = await monitorClient.GetAsync("/api/Game/99999/SubmissionSheet");
        Assert.Equal(HttpStatusCode.NotFound, submissionNotFoundResponse.StatusCode);
        output.WriteLine("  ✓ Non-existent game returns 404");

        // Test: Non-started game returns BadRequest
        var submissionFutureResponse = await monitorClient.GetAsync($"/api/Game/{futureGame.Id}/SubmissionSheet");
        Assert.Equal(HttpStatusCode.BadRequest, submissionFutureResponse.StatusCode);
        output.WriteLine("  ✓ Non-started game returns BadRequest");

        // Test: Non-monitor user returns Forbidden
        var submissionForbiddenResponse = await userClient.GetAsync($"/api/Game/{game.Id}/SubmissionSheet");
        Assert.Equal(HttpStatusCode.Forbidden, submissionForbiddenResponse.StatusCode);
        output.WriteLine("  ✓ Non-monitor user returns Forbidden");

        // Main test: Download and verify submission Excel content
        var submissionResponse = await monitorClient.GetAsync($"/api/Game/{game.Id}/SubmissionSheet");
        submissionResponse.EnsureSuccessStatusCode();

        // Verify Excel file format
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            submissionResponse.Content.Headers.ContentType?.MediaType);
        output.WriteLine("  ✓ Submission file has correct content type");

        // Parse and validate Excel structure
        await using var submissionStream = await submissionResponse.Content.ReadAsStreamAsync();
        Assert.True(submissionStream.Length > 0, "Submission Excel file should not be empty");
        output.WriteLine($"  ✓ Submission file size: {submissionStream.Length} bytes");

        using var submissionWorkbook = new NPOI.XSSF.UserModel.XSSFWorkbook(submissionStream);
        var submissionSheet = submissionWorkbook.GetSheetAt(0);

        // Verify submission sheet has proper row count
        // Expected: 1 header row + 3 submissions (Team 1: 2, Team 2: 1)
        int expectedMinRows = 1 + 3; // header + 3 submissions
        Assert.True(submissionSheet.PhysicalNumberOfRows >= expectedMinRows,
            $"Submission sheet should contain at least {expectedMinRows} rows (header + submissions), got {submissionSheet.PhysicalNumberOfRows}");
        output.WriteLine($"  ✓ Submission sheet has {submissionSheet.PhysicalNumberOfRows} rows (expected >= {expectedMinRows})");
    }

    /// <summary>
    /// Helper: Extract string value from cell, handling different cell types
    /// </summary>
    private string GetCellStringValue(ICell? cell)
    {
        if (cell == null)
            return string.Empty;

        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue ?? "",
            CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            _ => ""
        };
    }

    /// <summary>
    /// Helper: Parse rank value from Excel (numeric or "-" for unranked)
    /// </summary>
    private int ParseRank(string rankValue)
    {
        if (rankValue == "-" || rankValue == "<empty>")
            return 0; // Unranked

        if (int.TryParse(rankValue, out var rank))
            return rank;

        return 0;
    }

    #region Helper Methods

    private static readonly JsonSerializerOptions ScoreboardJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static DateTimeOffset ConvertToDateTime(long milliseconds)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.MinValue;
        }
    }

    private async Task<SeededGame> CreateGameWithBloodBonus(
        string title,
        long bloodBonusValue)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();

        var now = DateTimeOffset.UtcNow;

        var game = new Game
        {
            Title = title,
            Summary = "Test game",
            Content = "Test content",
            Hidden = false,
            PracticeMode = false,
            AcceptWithoutReview = true,
            WriteupRequired = false,
            TeamMemberCountLimit = 0,
            ContainerCountLimit = 3,
            StartTimeUtc = now.AddMinutes(-10),
            EndTimeUtc = now.AddHours(3),
            WriteupDeadline = now.AddHours(3),
            BloodBonus = BloodBonus.FromValue(bloodBonusValue)
        };

        var created = await gameRepository.CreateGame(game, CancellationToken.None)
                      ?? throw new InvalidOperationException($"Failed to create game {title}");

        return new SeededGame(created.Id, created.Title, created.StartTimeUtc, created.EndTimeUtc);
    }

    // Convert blood bonus factors to packed
    // Format: (first << 20) + (second << 10) + third
    // Factors are stored as (factor - 1.0) * 1000
    static long PackBloodFactor(float factor) =>
        (long)MathF.Round((factor - 1.0f) * 1000f, MidpointRounding.AwayFromZero);

    private long packBloods(float first, float second, float third) =>
        (PackBloodFactor(first) << 20) + (PackBloodFactor(second) << 10) + PackBloodFactor(third);

    private async Task<SeededChallenge> CreateChallenge(
        int gameId,
        string title,
        string flag,
        int score)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        var game = await gameRepository.GetGameById(gameId, CancellationToken.None)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        GameChallenge challenge = new()
        {
            Title = title,
            Content = "Challenge content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = score,
            MinScoreRate = 1.0, // Static scoring by default
            Difficulty = 1,
            Game = game,
            GameId = game.Id
        };

        FlagContext flagContext = new() { Flag = flag, Challenge = challenge };
        challenge.Flags.Add(flagContext);

        await challengeRepository.CreateChallenge(game, challenge, CancellationToken.None);

        return new SeededChallenge(challenge.Id, challenge.Title, flag);
    }

    private async Task<SeededChallenge> CreateDynamicChallenge(
        int gameId,
        string title,
        string flag,
        int originalScore,
        double minScoreRate,
        double difficulty)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        var game = await gameRepository.GetGameById(gameId, CancellationToken.None)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        GameChallenge challenge = new()
        {
            Title = title,
            Content = "Dynamic challenge content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = originalScore,
            MinScoreRate = minScoreRate,
            Difficulty = difficulty,
            Game = game,
            GameId = game.Id
        };

        FlagContext flagContext = new() { Flag = flag, Challenge = challenge };
        challenge.Flags.Add(flagContext);

        await challengeRepository.CreateChallenge(game, challenge, CancellationToken.None);

        return new SeededChallenge(challenge.Id, challenge.Title, flag);
    }

    private async Task<Division> CreateDivision(int gameId, DivisionCreateModel model)
    {
        using var scope = factory.Services.CreateScope();

        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var divisionRepository = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();

        var game = await gameRepository.GetGameById(gameId, CancellationToken.None)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");
        var division = await divisionRepository.CreateDivision(game, model, CancellationToken.None);
        Assert.NotNull(division);

        return division;
    }

    private async Task<List<(TestDataSeeder.SeededUser user, TestDataSeeder.SeededTeam team, HttpClient client)>>
        CreateMultipleTeams(
            int count,
            string namePrefix)
    {
        var teams = new List<(TestDataSeeder.SeededUser, TestDataSeeder.SeededTeam, HttpClient)>();
        var password = $"{namePrefix}@Pass123";

        for (int i = 0; i < count; i++)
        {
            var userName = TestDataSeeder.RandomName();
            var user = await TestDataSeeder.CreateUserAsync(
                factory.Services,
                userName,
                password
            );

            var team = await TestDataSeeder.CreateTeamAsync(
                factory.Services,
                user.Id,
                $"{namePrefix} {i + 1}"
            );

            var client = factory.CreateClient();
            using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                       new LoginModel { UserName = user.UserName, Password = password }))
            {
                loginResponse.EnsureSuccessStatusCode();
            }

            teams.Add((user, team, client));
        }

        return teams;
    }

    private async Task<(TestDataSeeder.SeededUser user, TestDataSeeder.SeededTeam team, HttpClient client)>
        CreateTeamInDivision(
            string teamName,
            int gameId,
            int divisionId,
            string inviteCode)
    {
        var password = $"{teamName}@Pass123";
        var userName = TestDataSeeder.RandomName();

        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services,
            userName,
            password
        );

        var team = await TestDataSeeder.CreateTeamAsync(
            factory.Services,
            user.Id,
            teamName
        );

        var client = factory.CreateClient();
        using (var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                   new LoginModel { UserName = user.UserName, Password = password }))
        {
            loginResponse.EnsureSuccessStatusCode();
        }

        using (var joinResponse = await client.PostAsJsonAsync($"/api/Game/{gameId}",
                   new GameJoinModel { TeamId = team.Id, DivisionId = divisionId, InviteCode = inviteCode }))
        {
            joinResponse.EnsureSuccessStatusCode();
        }

        return (user, team, client);
    }

    private async Task JoinGame(
        (TestDataSeeder.SeededUser user, TestDataSeeder.SeededTeam team, HttpClient client) team, int gameId)
    {
        using var joinResponse = await team.client.PostAsJsonAsync($"/api/Game/{gameId}",
            new GameJoinModel { TeamId = team.team.Id });
        joinResponse.EnsureSuccessStatusCode();
    }

    private async Task JoinGameAndSolve(
        (TestDataSeeder.SeededUser user, TestDataSeeder.SeededTeam team, HttpClient client) team,
        int gameId,
        List<(int challengeId, string flag)> solutions)
    {
        using (var joinResponse = await team.client.PostAsJsonAsync($"/api/Game/{gameId}",
                   new GameJoinModel { TeamId = team.team.Id }))
        {
            joinResponse.EnsureSuccessStatusCode();
        }

        foreach (var (challengeId, flag) in solutions)
        {
            await SubmitFlag(team.client, gameId, challengeId, flag);
            await Task.Delay(50); // Small delay between submissions
        }
    }

    private async Task<HttpResponseMessage> SubmitFlag(
        HttpClient client,
        int gameId,
        int challengeId,
        string flag,
        bool ensureSuccess = true)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/Game/{gameId}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = flag }
        );
        if (ensureSuccess)
            response.EnsureSuccessStatusCode();

        return response;
    }

    private async Task FlushScoreboardCache(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var cacheHelper = scope.ServiceProvider.GetRequiredService<CacheHelper>();
        await cacheHelper.FlushScoreboardCache(gameId, CancellationToken.None);
    }

    private async Task<ScoreboardSnapshot> GetScoreboard(
        HttpClient client,
        int gameId,
        IEnumerable<int>? expectedTeamIds = null,
        Func<ScoreboardSnapshot, bool>? readiness = null,
        int maxAttempts = 20,
        int delayMilliseconds = 100)
    {
        ScoreboardSnapshot? last = null;
        var expected = expectedTeamIds?.ToArray();
        readiness ??= static _ => true;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            last = await FetchScoreboardAsync(client, gameId);

            var hasExpectedTeams = expected is null || expected.All(id => last.TeamsById.ContainsKey(id));
            if (hasExpectedTeams && readiness(last))
                return last;

            await Task.Delay(delayMilliseconds);
        }

        Assert.Fail($"Scoreboard for game {gameId} did not reach expected state after {maxAttempts} attempts.");
        return last!;
    }

    private async Task<ScoreboardSnapshot> FetchScoreboardAsync(HttpClient client, int gameId)
    {
        var response = await client.GetAsync($"/api/Game/{gameId}/Scoreboard");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<ScoreboardResponse>(stream, ScoreboardJsonOptions);
        Assert.NotNull(payload);

        return ScoreboardSnapshot.FromResponse(payload);
    }

    private static int GetTeamScore(ScoreboardSnapshot scoreboard, int teamId) =>
        scoreboard.GetTeamScore(teamId);

    private sealed class ScoreboardSnapshot
    {
        readonly IReadOnlyDictionary<int, ScoreboardTeam> _teamMap;
        readonly IReadOnlyDictionary<int, ChallengeSnapshot> _challengeMap;

        ScoreboardSnapshot(
            DateTimeOffset updateTimeUtc,
            long bloodBonusValue,
            int challengeCount,
            IReadOnlyList<ScoreboardTeam> teams,
            IReadOnlyDictionary<int, ScoreboardTeam> teamMap,
            IReadOnlyDictionary<int, ChallengeSnapshot> challengeMap,
            IReadOnlyDictionary<int, DivisionSnapshot> divisions,
            IReadOnlyList<TimeLineItemSnapshot> timelines)
        {
            UpdateTimeUtc = updateTimeUtc;
            BloodBonusValue = bloodBonusValue;
            ChallengeCount = challengeCount;
            Teams = teams;
            this._teamMap = teamMap;
            this._challengeMap = challengeMap;
            Divisions = divisions;
            Timelines = timelines;
        }

        public DateTimeOffset UpdateTimeUtc { get; }
        public long BloodBonusValue { get; }
        public int ChallengeCount { get; }
        public IReadOnlyList<ScoreboardTeam> Teams { get; }
        public IReadOnlyDictionary<int, DivisionSnapshot> Divisions { get; }
        public IReadOnlyList<TimeLineItemSnapshot> Timelines { get; }
        public int TeamCount => _teamMap.Count;
        public IReadOnlyDictionary<int, ScoreboardTeam> TeamsById => _teamMap;
        public IEnumerable<ChallengeSnapshot> ChallengeList => _challengeMap.Values;

        public ScoreboardTeam GetTeam(int teamId) =>
            _teamMap.TryGetValue(teamId, out var team)
                ? team
                : throw new KeyNotFoundException($"Team {teamId} not found on scoreboard.");

        public int GetTeamScore(int teamId) => GetTeam(teamId).Score;

        public ChallengeSnapshot GetChallenge(int challengeId) =>
            _challengeMap.TryGetValue(challengeId, out var challenge)
                ? challenge
                : throw new KeyNotFoundException($"Challenge {challengeId} not found on scoreboard.");

        public static ScoreboardSnapshot FromResponse(ScoreboardResponse payload)
        {
            var teams = (payload.Items ?? new List<ScoreboardItemResponse>()).Select(ScoreboardTeam.FromResponse)
                .ToList();
            var teamMap = teams.ToDictionary(team => team.Id);

            var challengeMap = new Dictionary<int, ChallengeSnapshot>();
            if (payload.Challenges is not null)
            {
                foreach (var (categoryKey, challengeList) in payload.Challenges)
                {
                    var category = Enum.TryParse<ChallengeCategory>(categoryKey, true, out var parsedCategory)
                        ? parsedCategory
                        : ChallengeCategory.Misc;

                    foreach (var snapshot in challengeList.Select(challenge =>
                                 ChallengeSnapshot.FromResponse(challenge, category)))
                    {
                        challengeMap[snapshot.Id] = snapshot;
                    }
                }
            }

            var divisions = (payload.Divisions ?? new List<DivisionItemResponse>()).ToDictionary(
                division => division.Id,
                DivisionSnapshot.FromResponse);

            var timelines = (payload.Timelines ?? new List<TimeLineItemResponse>())
                .Select(TimeLineItemSnapshot.FromResponse).ToList();

            return new ScoreboardSnapshot(
                ConvertToDateTime(payload.UpdateTimeUtc),
                payload.BloodBonus,
                payload.ChallengeCount,
                teams,
                teamMap,
                challengeMap,
                divisions,
                timelines);
        }
    }

    private sealed record ScoreboardTeam(
        int Id,
        string Name,
        int Score,
        int Rank,
        int? DivisionId,
        int? DivisionRank,
        DateTimeOffset LastSubmissionTime,
        IReadOnlyList<SolvedChallenge> SolvedChallenges,
        string? Bio,
        string? Avatar)
    {
        public int SolvedCount => SolvedChallenges.Count;

        public static ScoreboardTeam FromResponse(ScoreboardItemResponse item)
        {
            var solved = (item.SolvedChallenges ?? []).Select(SolvedChallenge.FromResponse).ToList();
            return new ScoreboardTeam(
                item.Id,
                item.Name,
                item.Score,
                item.Rank,
                item.DivisionId,
                item.DivisionRank,
                ConvertToDateTime(item.LastSubmissionTime),
                solved,
                item.Bio,
                item.Avatar);
        }
    }

    private sealed record SolvedChallenge(
        int Id,
        int Score,
        SubmissionType Type,
        DateTimeOffset SubmitTimeUtc,
        string? UserName)
    {
        public static SolvedChallenge FromResponse(SolvedChallengeResponse response)
        {
            var type = Enum.TryParse<SubmissionType>(response.Type, true, out var parsed)
                ? parsed
                : SubmissionType.Unaccepted;

            return new SolvedChallenge(
                response.Id,
                response.Score,
                type,
                ConvertToDateTime(response.Time),
                response.UserName);
        }
    }

    private sealed record ChallengeSnapshot(
        int Id,
        ChallengeCategory Category,
        string Title,
        int Score,
        int SolvedCount,
        bool DisableBloodBonus,
        IReadOnlyList<BloodSnapshot> Bloods)
    {
        public static ChallengeSnapshot FromResponse(ChallengeInfoResponse response, ChallengeCategory category)
        {
            var bloods = (response.Bloods ?? new List<BloodResponse>()).Select(BloodSnapshot.FromResponse).ToList();
            return new ChallengeSnapshot(
                response.Id,
                category,
                response.Title,
                response.Score,
                response.Solved,
                response.DisableBloodBonus,
                bloods);
        }
    }

    private sealed record BloodSnapshot(int Id, string Name, string? Avatar, DateTimeOffset? SubmitTimeUtc)
    {
        public static BloodSnapshot FromResponse(BloodResponse response)
        {
            DateTimeOffset? submitTime = response.SubmitTimeUtc.HasValue
                ? ConvertToDateTime(response.SubmitTimeUtc.Value)
                : null;

            return new BloodSnapshot(response.Id, response.Name, response.Avatar, submitTime);
        }
    }

    private sealed record DivisionSnapshot(
        int Id,
        string Name,
        GamePermission DefaultPermissions,
        IReadOnlyDictionary<int, DivisionChallengeConfigSnapshot> ChallengeConfigs)
    {
        public static DivisionSnapshot FromResponse(DivisionItemResponse response)
        {
            var configs = (response.ChallengeConfigs ?? new Dictionary<string, DivisionChallengeItemResponse>())
                .Select(kvp =>
                {
                    var challengeId = int.TryParse(kvp.Key, out var parsedId)
                        ? parsedId
                        : kvp.Value.ChallengeId;
                    return DivisionChallengeConfigSnapshot.FromResponse(challengeId, kvp.Value);
                })
                .ToDictionary(cfg => cfg.ChallengeId);

            return new DivisionSnapshot(response.Id, response.Name, response.DefaultPermissions, configs);
        }
    }

    private sealed record DivisionChallengeConfigSnapshot(int ChallengeId, GamePermission Permissions)
    {
        public static DivisionChallengeConfigSnapshot FromResponse(int challengeId,
            DivisionChallengeItemResponse response)
            => new(challengeId, response.Permissions);
    }

    private sealed record TimeLineItemSnapshot(int DivisionId, IReadOnlyList<TopTimeLineSnapshot> Teams)
    {
        public static TimeLineItemSnapshot FromResponse(TimeLineItemResponse response)
        {
            var teams = (response.Teams ?? new List<TopTimeLineResponse>()).Select(TopTimeLineSnapshot.FromResponse)
                .ToList();
            return new TimeLineItemSnapshot(response.DivisionId ?? 0, teams);
        }
    }

    private sealed record TopTimeLineSnapshot(int Id, string Name, IReadOnlyList<TimeLineSnapshot> Items)
    {
        public static TopTimeLineSnapshot FromResponse(TopTimeLineResponse response)
        {
            var items = (response.Items ?? new List<TimeLineResponse>()).Select(TimeLineSnapshot.FromResponse).ToList();
            return new TopTimeLineSnapshot(response.Id, response.Name, items);
        }
    }

    private sealed record TimeLineSnapshot(DateTimeOffset Time, int Score)
    {
        public static TimeLineSnapshot FromResponse(TimeLineResponse response) =>
            new(ConvertToDateTime(response.Time), response.Score);
    }

    private sealed class ScoreboardResponse
    {
        public long UpdateTimeUtc { get; init; }
        public long BloodBonus { get; init; }
        public List<TimeLineItemResponse>? Timelines { get; init; }
        public List<ScoreboardItemResponse>? Items { get; init; }
        public List<DivisionItemResponse>? Divisions { get; init; }
        public Dictionary<string, List<ChallengeInfoResponse>>? Challenges { get; init; }
        public int ChallengeCount { get; init; }
    }

    private sealed class TimeLineItemResponse
    {
        public int? DivisionId { get; set; }
        public List<TopTimeLineResponse>? Teams { get; set; }
    }

    private sealed class TopTimeLineResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TimeLineResponse>? Items { get; set; }
    }

    private sealed class TimeLineResponse
    {
        public long Time { get; set; }
        public int Score { get; set; }
    }

    private sealed class ScoreboardItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int? DivisionId { get; set; }
        public string? Avatar { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public int? DivisionRank { get; set; }
        public long LastSubmissionTime { get; set; }
        public List<SolvedChallengeResponse>? SolvedChallenges { get; set; }
    }

    private sealed class SolvedChallengeResponse
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public long Time { get; set; }
    }

    private sealed class ChallengeInfoResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Solved { get; set; }
        public bool DisableBloodBonus { get; set; }
        public List<BloodResponse>? Bloods { get; set; }
    }

    private sealed class BloodResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public long? SubmitTimeUtc { get; set; }
    }

    private sealed class DivisionItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public GamePermission DefaultPermissions { get; set; }
        public Dictionary<string, DivisionChallengeItemResponse>? ChallengeConfigs { get; set; }
    }

    private sealed class DivisionChallengeItemResponse
    {
        public int ChallengeId { get; set; }
        public GamePermission Permissions { get; set; }
    }

    private record SeededGame(int Id, string Title, DateTimeOffset Start, DateTimeOffset End);

    private record SeededChallenge(int Id, string Title, string Flag);

    #endregion
}
