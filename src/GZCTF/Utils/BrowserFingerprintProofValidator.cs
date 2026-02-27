using System.Text.Json;

namespace GZCTF.Utils;

public sealed class BrowserFingerprintProof
{
    public int Version { get; set; }

    public string? Fingerprint { get; set; }

    public string? Nonce { get; set; }

    public string[]? SignalOrder { get; set; }

    public Dictionary<string, string>? Signals { get; set; }

    public int LieCount { get; set; }

    public int TrashCount { get; set; }

    public int ErrorCount { get; set; }

    public int HeadlessRating { get; set; }

    public int StealthRating { get; set; }

    public int LikeHeadlessRating { get; set; }

    public BrowserFingerprintResistanceProof? Resistance { get; set; }

    public string[]? SuspiciousSignals { get; set; }
}

public sealed class BrowserFingerprintResistanceProof
{
    public string? Privacy { get; set; }

    public string? Mode { get; set; }

    public string? Extension { get; set; }
}

public static class BrowserFingerprintProofValidator
{
    private const int CurrentVersion = 1;
    private const int ExtremeLieCountThreshold = 320;
    private const int ExtremeTrashCountThreshold = 32;
    private const int ExtremeErrorCountThreshold = 48;
    private const int HeadlessRatingThreshold = 55;
    private const int StealthRatingThreshold = 45;
    private const int LikeHeadlessRatingThreshold = 80;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static BrowserFingerprintProof? Parse(string? proofJson)
    {
        if (string.IsNullOrWhiteSpace(proofJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BrowserFingerprintProof>(proofJson, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsTrusted(BrowserFingerprintProof proof, string fingerprint, out string reason)
    {
        if (proof.Version != CurrentVersion)
        {
            reason = "Unsupported fingerprint proof version";
            return false;
        }

        if (!string.Equals(proof.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            reason = "Fingerprint proof mismatch";
            return false;
        }

        if (!IsInRange(proof.LieCount, 0, 2048)
            || !IsInRange(proof.TrashCount, 0, 2048)
            || !IsInRange(proof.ErrorCount, 0, 2048)
            || !IsInRange(proof.HeadlessRating, 0, 100)
            || !IsInRange(proof.StealthRating, 0, 100)
            || !IsInRange(proof.LikeHeadlessRating, 0, 100))
        {
            reason = "Fingerprint proof contains out-of-range metrics";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(proof.Resistance?.Extension))
        {
            reason = "Fingerprint resistance extension detected";
            return false;
        }

        if (IsPrivacyResistanceDetected(proof.Resistance, proof.Signals))
        {
            reason = "Fingerprint privacy resistance mode detected";
            return false;
        }

        var severeSignals = 0;

        if (proof.HeadlessRating >= HeadlessRatingThreshold)
            severeSignals += 2;

        if (proof.StealthRating >= StealthRatingThreshold)
            severeSignals += 2;

        if (proof.LikeHeadlessRating >= LikeHeadlessRatingThreshold)
            severeSignals += 2;

        if (proof.LieCount >= ExtremeLieCountThreshold)
            severeSignals += 1;

        if (proof.TrashCount >= ExtremeTrashCountThreshold)
            severeSignals += 1;

        if (proof.ErrorCount >= ExtremeErrorCountThreshold)
            severeSignals += 1;

        if (HasInconsistentCrossContextSignals(proof.Signals))
            severeSignals += 1;

        if (severeSignals >= 3)
        {
            reason = "Strong fingerprint forgery indicators detected";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static bool HasValidChallengeSignals(BrowserFingerprintProof proof, IReadOnlyCollection<string> requiredSignals,
        out string reason)
    {
        if (proof.Signals is null || proof.SignalOrder is null)
        {
            reason = "Missing challenge signals";
            return false;
        }

        if (proof.SignalOrder.Length != requiredSignals.Count || proof.Signals.Count != requiredSignals.Count)
        {
            reason = "Challenge signal count mismatch";
            return false;
        }

        var expectedOrder = requiredSignals.ToArray();
        if (expectedOrder.Distinct(StringComparer.Ordinal).Count() != expectedOrder.Length)
        {
            reason = "Invalid challenge signal definition";
            return false;
        }

        for (var i = 0; i < expectedOrder.Length; i++)
        {
            if (!string.Equals(proof.SignalOrder[i], expectedOrder[i], StringComparison.Ordinal))
            {
                reason = "Challenge signal order mismatch";
                return false;
            }
        }

        foreach (var key in expectedOrder)
        {
            if (!proof.Signals.ContainsKey(key))
            {
                reason = $"Missing challenge signal: {key}";
                return false;
            }
        }

        if (!TryGetIntSignal(proof.Signals, "lie_count", out var lieCount)
            || !TryGetIntSignal(proof.Signals, "trash_count", out var trashCount)
            || !TryGetIntSignal(proof.Signals, "error_count", out var errorCount)
            || !TryGetIntSignal(proof.Signals, "headless_rating", out var headlessRating)
            || !TryGetIntSignal(proof.Signals, "stealth_rating", out var stealthRating)
            || !TryGetIntSignal(proof.Signals, "like_headless_rating", out var likeHeadlessRating))
        {
            reason = "Invalid numeric challenge signal";
            return false;
        }

        if (lieCount != proof.LieCount
            || trashCount != proof.TrashCount
            || errorCount != proof.ErrorCount
            || headlessRating != proof.HeadlessRating
            || stealthRating != proof.StealthRating
            || likeHeadlessRating != proof.LikeHeadlessRating)
        {
            reason = "Challenge signal metric mismatch";
            return false;
        }

        if (!TryGetBooleanSignal(proof.Signals, "platform_consistent", out _)
            || !TryGetBooleanSignal(proof.Signals, "ua_consistent", out _)
            || !TryGetBooleanSignal(proof.Signals, "webgl_consistent", out _))
        {
            reason = "Invalid consistency challenge signal";
            return false;
        }

        var resistanceExtensionSignal = NormalizeSignalValue(GetSignalValue(proof.Signals, "resistance_extension"));
        var resistancePrivacySignal = NormalizeSignalValue(GetSignalValue(proof.Signals, "resistance_privacy"));
        var resistanceExtension = NormalizeSignalValue(proof.Resistance?.Extension);
        var resistancePrivacy = NormalizeSignalValue(proof.Resistance?.Privacy);

        if (!string.Equals(resistanceExtensionSignal, resistanceExtension, StringComparison.Ordinal))
        {
            reason = "Challenge signal resistance extension mismatch";
            return false;
        }

        if (!string.Equals(resistancePrivacySignal, resistancePrivacy, StringComparison.Ordinal))
        {
            reason = "Challenge signal resistance privacy mismatch";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsInRange(int value, int min, int max) => value >= min && value <= max;

    private static bool IsPrivacyResistanceDetected(BrowserFingerprintResistanceProof? resistance,
        Dictionary<string, string>? signals)
    {
        var privacy = NormalizeSignalValue(resistance?.Privacy) ?? NormalizeSignalValue(GetSignalValue(signals, "resistance_privacy"));
        var mode = NormalizeSignalValue(resistance?.Mode);

        if (!string.IsNullOrWhiteSpace(privacy))
            return true;

        if (!string.IsNullOrWhiteSpace(mode))
            return true;

        return false;
    }

    private static bool HasInconsistentCrossContextSignals(Dictionary<string, string>? signals)
    {
        if (signals is null || signals.Count == 0)
            return false;

        static bool IsFalseSignal(Dictionary<string, string> data, string key) =>
            data.TryGetValue(key, out var value) && (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase));

        var inconsistentCount = 0;
        if (IsFalseSignal(signals, "platform_consistent"))
            inconsistentCount++;
        if (IsFalseSignal(signals, "ua_consistent"))
            inconsistentCount++;
        if (IsFalseSignal(signals, "webgl_consistent"))
            inconsistentCount++;

        return inconsistentCount >= 2;
    }

    private static bool TryGetIntSignal(Dictionary<string, string>? signals, string key, out int value)
    {
        value = 0;
        if (signals is null || !signals.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        if (!int.TryParse(raw, out value))
            return false;

        return IsInRange(value, 0, 2048);
    }

    private static bool TryGetBooleanSignal(Dictionary<string, string>? signals, string key, out bool value)
    {
        value = false;
        if (signals is null || !signals.TryGetValue(key, out var raw))
            return false;

        var normalized = raw.Trim();
        if (normalized == "1")
        {
            value = true;
            return true;
        }

        if (normalized == "0")
        {
            value = false;
            return true;
        }

        return bool.TryParse(normalized, out value);
    }

    private static string? GetSignalValue(Dictionary<string, string>? signals, string key)
    {
        if (signals is null || !signals.TryGetValue(key, out var value))
            return null;
        return value;
    }

    private static string? NormalizeSignalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }
}
