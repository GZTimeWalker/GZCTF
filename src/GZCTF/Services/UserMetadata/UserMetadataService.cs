using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using GZCTF.Extensions.Startup;
using GZCTF.Models.Internal;
using InternalUserMetadataField = GZCTF.Models.Internal.UserMetadataField;

namespace GZCTF.Services;

public sealed class UserMetadataValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public class UserMetadataService(
    IOAuthProviderManager oauthManager) : IUserMetadataService
{
    static readonly EmailAddressAttribute EmailAttribute = new();
    static readonly PhoneAttribute PhoneAttribute = new();

    public async Task<UserMetadataValidationResult> ValidateAsync(
        IDictionary<string, string?>? incoming,
        IDictionary<string, string>? existing,
        bool allowLockedWrites,
        bool enforceLockedRequirements,
        CancellationToken token = default)
    {
        var fields = await oauthManager.GetUserMetadataFieldsAsync(token);
        var result = new UserMetadataValidationResult();
        var source = incoming ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var current = existing is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            var hasIncoming = source.TryGetValue(field.Key, out var providedValue);
            var canWriteField = !field.Locked || allowLockedWrites;
            string? candidate = hasIncoming && canWriteField ? providedValue : current.GetValueOrDefault(field.Key);

            var enforceRequirement = field.Required && (!field.Locked || enforceLockedRequirements);

            if (!canWriteField && hasIncoming)
                // Ignore attempts to set locked fields when not permitted
                candidate = current.GetValueOrDefault(field.Key);

            var validation = ValidateField(field, candidate);

            if (!validation.IsValid)
            {
                if (enforceRequirement || !string.IsNullOrWhiteSpace(candidate))
                    result.Errors.Add(validation.ErrorMessage);
                continue;
            }

            if (string.IsNullOrWhiteSpace(validation.NormalizedValue))
            {
                if (enforceRequirement)
                    result.Errors.Add($"Field '{field.DisplayName}' is required.");

                continue;
            }

            result.Values[field.Key] = validation.NormalizedValue!;
        }

        foreach (var (key, value) in current)
        {
            if (result.Values.ContainsKey(key))
                continue;

            if (fields.Any(f => f.Key == key))
                continue;

            if (!string.IsNullOrWhiteSpace(value))
                result.Values[key] = value;
        }

        return result;
    }

    public async Task<IReadOnlyList<InternalUserMetadataField>> GetFieldsAsync(CancellationToken token = default)
    {
        var fields = await oauthManager.GetUserMetadataFieldsAsync(token);
        return fields;
    }

    static FieldValidationResult ValidateField(InternalUserMetadataField field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return FieldValidationResult.Success(null);

        var trimmed = value.Trim();

        if (field.MaxLength is > 0 && trimmed.Length > field.MaxLength)
            return FieldValidationResult.Failure($"Field '{field.DisplayName}' exceeds max length {field.MaxLength}.");

        if (!string.IsNullOrWhiteSpace(field.Pattern))
        {
            var regex = new Regex(field.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            if (!regex.IsMatch(trimmed))
                return FieldValidationResult.Failure($"Field '{field.DisplayName}' does not match required pattern.");
        }

        return field.Type switch
        {
            UserMetadataFieldType.Number => ValidateNumber(field, trimmed),
            UserMetadataFieldType.Email => ValidateEmail(field, trimmed),
            UserMetadataFieldType.Url => ValidateUrl(field, trimmed),
            UserMetadataFieldType.Phone => ValidatePhone(field, trimmed),
            UserMetadataFieldType.Date => ValidateDate(field, trimmed),
            UserMetadataFieldType.Select => ValidateSelect(field, trimmed),
            _ => FieldValidationResult.Success(trimmed)
        };
    }

    static FieldValidationResult ValidateNumber(InternalUserMetadataField field, string value)
    {
        if (!int.TryParse(value, out var number))
            return FieldValidationResult.Failure($"Field '{field.DisplayName}' must be a number.");

        if (field.MinValue.HasValue && number < field.MinValue.Value)
            return FieldValidationResult.Failure($"Field '{field.DisplayName}' must be >= {field.MinValue}.");

        if (field.MaxValue.HasValue && number > field.MaxValue.Value)
            return FieldValidationResult.Failure($"Field '{field.DisplayName}' must be <= {field.MaxValue}.");

        return FieldValidationResult.Success(number.ToString());
    }

    static FieldValidationResult ValidateEmail(InternalUserMetadataField field, string value)
        => EmailAttribute.IsValid(value)
            ? FieldValidationResult.Success(value)
            : FieldValidationResult.Failure($"Field '{field.DisplayName}' must be a valid email.");

    static FieldValidationResult ValidateUrl(InternalUserMetadataField field, string value)
        => Uri.TryCreate(value, UriKind.Absolute, out _)
            ? FieldValidationResult.Success(value)
            : FieldValidationResult.Failure($"Field '{field.DisplayName}' must be a valid URL.");

    static FieldValidationResult ValidatePhone(InternalUserMetadataField field, string value)
        => PhoneAttribute.IsValid(value)
            ? FieldValidationResult.Success(value)
            : FieldValidationResult.Failure($"Field '{field.DisplayName}' must be a valid phone number.");

    static FieldValidationResult ValidateDate(InternalUserMetadataField field, string value)
        => DateOnly.TryParse(value, out var parsed)
            ? FieldValidationResult.Success(parsed.ToString("O"))
            : FieldValidationResult.Failure($"Field '{field.DisplayName}' must be a valid date.");

    static FieldValidationResult ValidateSelect(InternalUserMetadataField field, string value)
    {
        if (field.Options is null || field.Options.Count == 0)
            return FieldValidationResult.Failure($"Field '{field.DisplayName}' has no options configured.");

        return field.Options.Contains(value)
            ? FieldValidationResult.Success(value)
            : FieldValidationResult.Failure($"Field '{field.DisplayName}' must be one of the provided options.");
    }

    private sealed record FieldValidationResult(bool IsValid, string? NormalizedValue, string ErrorMessage)
    {
        public static FieldValidationResult Success(string? normalizedValue) =>
            new(true, normalizedValue, string.Empty);

        public static FieldValidationResult Failure(string errorMessage) =>
            new(false, null, errorMessage);
    }
}
