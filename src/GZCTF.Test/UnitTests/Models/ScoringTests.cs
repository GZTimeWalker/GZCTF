using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Services;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

/// <summary>
/// Tests for the dynamic score curve and the blood bonus, both stored/derived
/// from <see cref="GameChallenge" /> and <see cref="Game" /> and only partially
/// covered by the HTTP-level integration tests.
/// </summary>
public class ScoringTests
{
    #region CalculateChallengeScore

    // 1 solved challenge keeps the initial score, no share deceleration yet
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CalculateChallengeScore_AtMostOneAccepted_ReturnsOriginalScore(int acceptedCount) =>
        Assert.Equal(1000, GameChallenge.CalculateChallengeScore(1000, 0.25, 5, acceptedCount));

    [Fact]
    public void CalculateChallengeScore_FollowsTheDocumentedCurve()
    {
        // f(S, r, d, x) = floor(S * (r + (1 - r) * exp((1 - x) / d)))
        const int original = 1000;
        const double minRate = 0.25;
        const double difficulty = 5;

        var expected = (int)Math.Floor(original *
                                       (minRate + (1 - minRate) * Math.Exp((1 - 2) / difficulty)));

        Assert.Equal(expected, GameChallenge.CalculateChallengeScore(original, minRate, difficulty, 2));
    }

    [Fact]
    public void CalculateChallengeScore_IsMonotonicallyDecreasing()
    {
        var previous = int.MaxValue;

        for (var accepted = 1; accepted <= 200; accepted++)
        {
            var score = GameChallenge.CalculateChallengeScore(1000, 0.25, 5, accepted);

            Assert.True(score <= previous, $"score increased at {accepted}: {previous} -> {score}");
            previous = score;
        }
    }

    [Fact]
    public void CalculateChallengeScore_NeverDropsBelowMinScoreRate()
    {
        // with enough solves the curve converges to floor(S * r)
        var floorScore = (int)Math.Floor(1000 * 0.25);

        for (var accepted = 1; accepted <= 10_000; accepted += 97)
            Assert.True(GameChallenge.CalculateChallengeScore(1000, 0.25, 5, accepted) >= floorScore);
    }

    [Fact]
    public void CalculateChallengeScore_KeepsDifficultyEffect()
    {
        // a larger difficulty coefficient decelerates slower, so it scores higher
        var easy = GameChallenge.CalculateChallengeScore(1000, 0.25, 1, 10);
        var hard = GameChallenge.CalculateChallengeScore(1000, 0.25, 20, 10);

        Assert.True(easy < hard, $"expected difficulty=1 ({easy}) < difficulty=20 ({hard})");
    }

    [Fact]
    public void CurrentScore_UsesFirstSolveCount()
    {
        var challenge = new GameChallenge
        {
            Title = "score",
            Content = "score",
            OriginalScore = 500,
            MinScoreRate = 0.5,
            Difficulty = 4
        };

        // no solves -> original score
        Assert.Equal(500, challenge.CurrentScore);
    }

    #endregion

    #region BloodBonus

    [Fact]
    public void BloodBonus_DefaultValue_IsFiveThreeOnePermille()
    {
        var bonus = new BloodBonus(BloodBonus.DefaultValue);

        Assert.Equal(50, bonus.FirstBlood);
        Assert.Equal(30, bonus.SecondBlood);
        Assert.Equal(10, bonus.ThirdBlood);
    }

    [Fact]
    public void BloodBonus_Factors_AddOneToThePermilleValue()
    {
        var bonus = new BloodBonus(BloodBonus.DefaultValue);

        Assert.Equal(1.05f, bonus.FirstBloodFactor);
        Assert.Equal(1.03f, bonus.SecondBloodFactor);
        Assert.Equal(1.01f, bonus.ThirdBloodFactor);
    }

    [Fact]
    public void BloodBonus_ZeroValue_ReportsNoBonus()
    {
        var bonus = BloodBonus.FromValue(0);

        Assert.True(bonus.NoBonus);
        Assert.Equal(0, bonus.FirstBlood);
        Assert.Equal(0, bonus.SecondBlood);
        Assert.Equal(0, bonus.ThirdBlood);
        Assert.Equal(1.0f, bonus.ThirdBloodFactor);
    }

    [Fact]
    public void BloodBonus_EncodesAndDecodesEachSlotIndependently()
    {
        // 1000 permille is the largest accepted slot value
        var bonus = BloodBonus.FromValue((1000L << 20) | (512L << 10) | 1);

        Assert.Equal(1000, bonus.FirstBlood);
        Assert.Equal(512, bonus.SecondBlood);
        Assert.Equal(1, bonus.ThirdBlood);
        Assert.False(bonus.NoBonus);
    }

    [Theory]
    [InlineData(1001L << 20)] // first blood slot exceeds the 1000 permille cap
    [InlineData(1001L << 10)] // second blood slot exceeds the cap
    [InlineData(1023L)] // third blood slot is the largest value that still does not fit
    [InlineData(-1001L)] // negative input puts garbage above the cap in every slot
    public void BloodBonus_OutOfRangeSlot_FallsBackToDefault(long invalid)
    {
        var bonus = BloodBonus.FromValue(invalid);

        Assert.Equal(BloodBonus.DefaultValue, bonus.Val);
        Assert.Equal(50, bonus.FirstBlood);
        Assert.Equal(30, bonus.SecondBlood);
        Assert.Equal(10, bonus.ThirdBlood);
        Assert.False(bonus.NoBonus);
    }

    [Fact]
    public void BloodBonus_AcceptsLargestInRangeSlot()
    {
        // 1000 permille is the documented maximum slot value
        var bonus = BloodBonus.FromValue((1000L << 20) | (1000L << 10) | 1000L);

        Assert.Equal(1000, bonus.FirstBlood);
        Assert.Equal(1000, bonus.SecondBlood);
        Assert.Equal(1000, bonus.ThirdBlood);
        Assert.Equal(2.0f, bonus.FirstBloodFactor);
    }

    [Fact]
    public void BloodBonus_RoundTripsThroughGame()
    {
        var game = new Game { Title = "round trip" };
        var custom = BloodBonus.FromValue((100L << 20) | (10L << 10) | 1);

        game.BloodBonus = custom;

        Assert.Equal(custom.Val, game.BloodBonusValue);
        Assert.Equal(custom.FirstBlood, game.BloodBonus.FirstBlood);
        Assert.Equal(custom.SecondBlood, game.BloodBonus.SecondBlood);
        Assert.Equal(custom.ThirdBlood, game.BloodBonus.ThirdBlood);
    }

    #endregion

    #region Blood bonus validation

    // IsValid backs both the read-path fallback and the write-path model check
    [Theory]
    [InlineData(0L, true)]
    [InlineData(10L, true)] // third blood only
    [InlineData(BloodBonus.DefaultValue, true)]
    [InlineData((1000L << 20) | (1000L << 10) | 1000L, true)] // every slot at the cap
    [InlineData(1001L << 20, false)] // first blood above the cap
    [InlineData(1001L << 10, false)] // second blood above the cap
    [InlineData(1001L, false)] // third blood above the cap
    [InlineData(1023L, false)] // fits the 10-bit slot but exceeds the cap
    [InlineData(1L << 30, false)] // bits outside the three slots
    [InlineData(-1L, false)]
    [InlineData(long.MinValue, false)]
    public void IsValid_MatchesThePackedLayout(long value, bool expected) =>
        Assert.Equal(expected, BloodBonus.IsValid(value));

    [Theory]
    [InlineData(0L)]
    [InlineData(BloodBonus.DefaultValue)]
    [InlineData((1000L << 20) | (1000L << 10) | 1000L)]
    [InlineData(1023L)]
    [InlineData(1001L << 20)]
    [InlineData(-1L)]
    [InlineData(1L << 30)]
    public void IsValid_AgreesWithFromValue(long value) =>
        Assert.Equal(BloodBonus.IsValid(value), BloodBonus.FromValue(value).Val == value);

    [Theory]
    [InlineData(BloodBonus.DefaultValue, true)]
    [InlineData((1000L << 20) | (1000L << 10) | 1000L, true)]
    [InlineData(1023L, false)]
    [InlineData(1001L << 20, false)]
    [InlineData(-1L, false)]
    public void BloodBonusValueAttribute_RejectsUnpackableValue(long value, bool expected)
    {
        var model = new GameInfoModel { BloodBonusValue = value };
        var context = new ValidationContext(model) { MemberName = nameof(GameInfoModel.BloodBonusValue) };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateProperty(model.BloodBonusValue, context, results);

        Assert.Equal(expected, valid);
        Assert.Equal(expected ? 0 : 1, results.Count);
    }

    #endregion

    #region FlagChecker worker count

    [Theory]
    [InlineData(1.0, 64, 1)] // RAM < 2GiB
    [InlineData(1.99, 64, 1)] // RAM < 2GiB
    [InlineData(32.0, 3, 1)] // CPU <= 3
    [InlineData(2.0, 4, 2)] // RAM >= 2GiB but < 4GiB
    [InlineData(3.99, 64, 2)] // RAM < 4GiB
    [InlineData(32.0, 6, 2)] // CPU <= 6
    [InlineData(4.0, 7, 4)] // both thresholds cleared
    [InlineData(128.0, 128, 4)] // capped at the maximum
    public void ResolveWorkerCount_FollowsTheDocumentedTiers(double memoryGiB, int cpuCount, int expected) =>
        Assert.Equal(expected, FlagChecker.ResolveWorkerCount(memoryGiB, cpuCount));

    [Fact]
    public void GetWorkerCount_UsesTheCurrentMachineAndStaysInRange()
    {
        var count = FlagChecker.GetWorkerCount();

        Assert.InRange(count, 1, 4);
    }

    #endregion
}
