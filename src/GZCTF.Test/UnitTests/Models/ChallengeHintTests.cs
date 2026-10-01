using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Transfer;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

public class ChallengeHintTests
{
    [Fact]
    public void NewHints_DefaultToHiddenInParticipantResponse()
    {
        var challenge = new GameChallenge();
        challenge.Update(new() { Hints = ["secret hint", "another secret"] });

        Assert.Equal(new[] { false, false }, challenge.GetHintEnabled());
        var detail = ChallengeDetailModel.FromInstance(new GameInstance { Challenge = challenge }, 0);
        Assert.Empty(detail.Hints!);
        Assert.Equal(2, ChallengeEditDetailModel.FromChallenge(challenge).Hints.Count);
    }

    [Fact]
    public void OnlyEnabledNonEmptyHints_AreReturnedToParticipants()
    {
        var challenge = new GameChallenge
        {
            Hints = ["hidden", "released", "   "], HintEnabled = [false, true, true]
        };

        Assert.Equal(new[] { "released" }, challenge.GetReleasedHints());
        challenge.Update(new() { HintEnabled = [false, false, false] });
        Assert.Empty(challenge.GetReleasedHints());
    }

    [Fact]
    public void LegacyDatabaseHints_KeepVisibilityUntilDisabled()
    {
        var challenge = new GameChallenge { Hints = ["legacy hint"], HintEnabled = null };
        Assert.Equal(new[] { "legacy hint" }, challenge.GetReleasedHints());
        Assert.Equal(new[] { true }, ChallengeEditDetailModel.FromChallenge(challenge).HintEnabled);

        challenge.Update(new() { Hints = ["legacy hint", "new hidden hint"] });
        Assert.Equal(new[] { true, false }, challenge.HintEnabled);
        Assert.Equal(new[] { "legacy hint" }, challenge.GetReleasedHints());
    }

    [Fact]
    public void TextOnlyUpdates_PreserveStateAcrossDeletionAndReordering()
    {
        var challenge = new GameChallenge
        {
            Hints = ["hidden", "released", "duplicate", "duplicate"],
            HintEnabled = [false, true, false, true]
        };
        challenge.Update(new() { Hints = ["duplicate", "released", "duplicate", "new"] });
        Assert.Equal(new[] { false, true, true, false }, challenge.HintEnabled);
        Assert.Equal(new[] { "released", "duplicate" }, challenge.GetReleasedHints());
    }

    [Fact]
    public void UnrelatedUpdates_DoNotChangeHintState()
    {
        var challenge = new GameChallenge { Hints = ["hint"], HintEnabled = [false] };
        challenge.Update(new() { Title = "Updated title" });
        Assert.Equal(new[] { false }, challenge.HintEnabled);
        Assert.Empty(challenge.GetReleasedHints());
    }

    [Fact]
    public void ExportImport_PreservesAllHintsAndTheirReleaseState()
    {
        var challenge = new GameChallenge { Hints = ["hidden", "released"], HintEnabled = [false, true] };
        var imported = challenge.ToTransfer().ToChallenge();
        Assert.Equal(challenge.Hints, imported.Hints);
        Assert.Equal(challenge.HintEnabled, imported.HintEnabled);
        Assert.Equal(new[] { "released" }, imported.GetReleasedHints());
    }

    [Fact]
    public void LegacyImports_DefaultToDisabledHints()
    {
        var imported = new TransferChallenge { Hints = ["imported hint"] }.ToChallenge();
        Assert.Equal(new[] { false }, imported.HintEnabled);
        Assert.Empty(imported.GetReleasedHints());
    }

    [Fact]
    public void TransferValidation_HandlesPrimitiveReleaseStateArrays()
    {
        var transfer = new GameChallenge
            { Title = "Hint challenge", Content = "Challenge content", Hints = ["hidden", "released"], HintEnabled = [false, true] }
            .ToTransfer();
        Assert.True(TransferValidator.TryValidate(transfer, out var errors));
        Assert.Empty(errors);
        transfer.HintEnabled = [true];
        Assert.False(TransferValidator.TryValidate(transfer, out errors));
        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData(false, "hint")]
    [InlineData(true, " ")]
    public void InvalidReleaseSettings_AreRejected(bool matchingLength, string content)
    {
        var model = new ChallengeUpdateModel
        {
            Hints = [content], HintEnabled = matchingLength ? [true] : []
        };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.NotEmpty(errors);
    }
}
