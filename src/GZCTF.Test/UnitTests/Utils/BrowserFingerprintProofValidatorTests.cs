using System.Collections.Generic;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Utils;

public class BrowserFingerprintProofValidatorTests
{
    private static readonly string[] RequiredSignals =
    [
        "lie_count",
        "trash_count",
        "error_count",
        "headless_rating",
        "stealth_rating",
        "like_headless_rating",
        "platform_consistent",
        "ua_consistent",
        "webgl_consistent",
        "resistance_extension",
        "resistance_privacy"
    ];

    [Fact]
    public void IsTrusted_ShouldReturnTrue_ForCleanProof()
    {
        var fingerprint = new string('a', 64);
        var proof = CreateProof(fingerprint);

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.True(trusted);
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void IsTrusted_ShouldReturnFalse_ForBraveStrictProof()
    {
        var fingerprint = new string('b', 64);
        var proof = CreateProof(fingerprint);
        proof.Resistance = new BrowserFingerprintResistanceProof
        {
            Privacy = "Brave",
            Mode = "strict"
        };

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.False(trusted);
        Assert.Equal("Fingerprint privacy resistance mode detected", reason);
    }

    [Fact]
    public void IsTrusted_ShouldReturnFalse_ForBraveAllowProof()
    {
        var fingerprint = new string('e', 64);
        var proof = CreateProof(fingerprint);
        proof.LieCount = 188;
        proof.TrashCount = 3;
        proof.HeadlessRating = 33;
        proof.StealthRating = 20;
        proof.LikeHeadlessRating = 44;
        proof.Resistance = new BrowserFingerprintResistanceProof
        {
            Privacy = "Brave",
            Mode = "allow"
        };

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.False(trusted);
        Assert.Equal("Fingerprint privacy resistance mode detected", reason);
    }

    [Fact]
    public void IsTrusted_ShouldReturnFalse_ForExtensionSpoofingProof()
    {
        var fingerprint = new string('c', 64);
        var proof = CreateProof(fingerprint);
        proof.Resistance = new BrowserFingerprintResistanceProof
        {
            Extension = "FakeBrowser"
        };

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.False(trusted);
        Assert.Equal("Fingerprint resistance extension detected", reason);
    }

    [Fact]
    public void Parse_ShouldReturnNull_ForInvalidJson()
    {
        var proof = BrowserFingerprintProofValidator.Parse("{invalid");
        Assert.Null(proof);
    }

    [Fact]
    public void IsTrusted_ShouldAllowHighLieCount_WhenNoOtherStrongForgerySignals()
    {
        var fingerprint = new string('d', 64);
        var proof = CreateProof(fingerprint, lieCount: 197, trashCount: 1, headlessRating: 33, stealthRating: 20);

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.True(trusted);
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void IsTrusted_ShouldReturnFalse_WhenResistancePrivacySignalIndicatesProtection()
    {
        var fingerprint = new string('f', 64);
        var proof = CreateProof(fingerprint);
        proof.Signals = new Dictionary<string, string>
        {
            ["resistance_privacy"] = "Brave"
        };

        var trusted = BrowserFingerprintProofValidator.IsTrusted(proof, fingerprint, out var reason);

        Assert.False(trusted);
        Assert.Equal("Fingerprint privacy resistance mode detected", reason);
    }

    [Fact]
    public void HasValidChallengeSignals_ShouldReturnTrue_ForConsistentProof()
    {
        var fingerprint = new string('1', 64);
        var proof = CreateProof(fingerprint, lieCount: 7, trashCount: 1, errorCount: 2, headlessRating: 5,
            stealthRating: 3, likeHeadlessRating: 4);
        proof.SignalOrder = RequiredSignals;
        proof.Signals = CreateSignals(proof);

        var valid = BrowserFingerprintProofValidator.HasValidChallengeSignals(proof, RequiredSignals, out var reason);

        Assert.True(valid);
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void HasValidChallengeSignals_ShouldReturnFalse_ForMetricMismatch()
    {
        var fingerprint = new string('2', 64);
        var proof = CreateProof(fingerprint, lieCount: 10);
        proof.SignalOrder = RequiredSignals;
        proof.Signals = CreateSignals(proof);
        proof.Signals["lie_count"] = "11";

        var valid = BrowserFingerprintProofValidator.HasValidChallengeSignals(proof, RequiredSignals, out var reason);

        Assert.False(valid);
        Assert.Equal("Challenge signal metric mismatch", reason);
    }

    [Fact]
    public void HasValidChallengeSignals_ShouldReturnFalse_ForResistancePrivacyMismatch()
    {
        var fingerprint = new string('3', 64);
        var proof = CreateProof(fingerprint);
        proof.SignalOrder = RequiredSignals;
        proof.Signals = CreateSignals(proof);
        proof.Signals["resistance_privacy"] = "Brave";

        var valid = BrowserFingerprintProofValidator.HasValidChallengeSignals(proof, RequiredSignals, out var reason);

        Assert.False(valid);
        Assert.Equal("Challenge signal resistance privacy mismatch", reason);
    }

    private static BrowserFingerprintProof CreateProof(string fingerprint, int lieCount = 0, int trashCount = 0,
        int errorCount = 0, int headlessRating = 0, int stealthRating = 0, int likeHeadlessRating = 0) =>
        new()
        {
            Version = 1,
            Fingerprint = fingerprint,
            LieCount = lieCount,
            TrashCount = trashCount,
            ErrorCount = errorCount,
            HeadlessRating = headlessRating,
            StealthRating = stealthRating,
            LikeHeadlessRating = likeHeadlessRating,
            SuspiciousSignals = []
        };

    private static Dictionary<string, string> CreateSignals(BrowserFingerprintProof proof) => new()
    {
        ["lie_count"] = proof.LieCount.ToString(),
        ["trash_count"] = proof.TrashCount.ToString(),
        ["error_count"] = proof.ErrorCount.ToString(),
        ["headless_rating"] = proof.HeadlessRating.ToString(),
        ["stealth_rating"] = proof.StealthRating.ToString(),
        ["like_headless_rating"] = proof.LikeHeadlessRating.ToString(),
        ["platform_consistent"] = "1",
        ["ua_consistent"] = "1",
        ["webgl_consistent"] = "1",
        ["resistance_extension"] = proof.Resistance?.Extension ?? string.Empty,
        ["resistance_privacy"] = proof.Resistance?.Privacy ?? string.Empty
    };
}
