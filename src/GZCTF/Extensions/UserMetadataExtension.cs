using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace GZCTF.Extensions;

public static class UserMetadataExtension
{
    extension(UserMetadataFieldType type)
    {
        public UserMetadataFieldValueType GetFieldValueType() =>
            type switch
            {
                UserMetadataFieldType.Number => UserMetadataFieldValueType.Number,
                UserMetadataFieldType.Boolean => UserMetadataFieldValueType.Boolean,
                UserMetadataFieldType.Text => UserMetadataFieldValueType.String,
                UserMetadataFieldType.TextArea => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Email => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Url => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Phone => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Select => UserMetadataFieldValueType.String,
                UserMetadataFieldType.Date => UserMetadataFieldValueType.DateTime,
                UserMetadataFieldType.MultiSelect => UserMetadataFieldValueType.StringArray,
                UserMetadataFieldType.DateTime => UserMetadataFieldValueType.DateTime,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
    }

    extension(UserMetadataField field)
    {
        public bool Validate(JsonDocument? value, IStringLocalizer<Program> localizer,
            // Only set error when validation fails, so MaybeNullWhen(true)
            [MaybeNullWhen(true)] out string error)
        {
            error = null;

            // Handle null/undefined
            if (value is null or { RootElement.ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
            {
                if (!field.Required)
                    return true;

                error = localizer[nameof(Resources.Program.Model_FieldRequired)];
                return false;
            }

            var root = value.RootElement;

            // Type validation
            switch (field.Type)
            {
                case UserMetadataFieldType.Text:
                case UserMetadataFieldType.TextArea:
                case UserMetadataFieldType.Email:
                case UserMetadataFieldType.Url:
                case UserMetadataFieldType.Phone:
                    if (root.ValueKind != JsonValueKind.String)
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeString)];
                        return false;
                    }

                    string strVal = root.GetString() ?? "";

                    if (field.Required && string.IsNullOrWhiteSpace(strVal))
                    {
                        error = localizer[nameof(Resources.Program.Model_FieldRequired)];
                        return false;
                    }

                    if (field.MaxLength.HasValue && strVal.Length > field.MaxLength.Value)
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueExceedsMaxLength), field.MaxLength.Value];
                        return false;
                    }

                    if (field.CompiledPattern != null && !field.CompiledPattern.IsMatch(strVal))
                    {
                        error = localizer[nameof(Resources.Program.Model_ValuePatternMismatch)];
                        return false;
                    }

                    break;

                case UserMetadataFieldType.Number:
                    if (root.ValueKind != JsonValueKind.Number)
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeNumber)];
                        return false;
                    }

                    if (root.TryGetDouble(out double intVal))
                    {
                        if (field.MinValue.HasValue && intVal < field.MinValue.Value)
                        {
                            error = localizer[nameof(Resources.Program.Model_ValueMustBeAtLeast), field.MinValue.Value];
                            return false;
                        }

                        if (field.MaxValue.HasValue && intVal > field.MaxValue.Value)
                        {
                            error = localizer[nameof(Resources.Program.Model_ValueMustBeAtMost), field.MaxValue.Value];
                            return false;
                        }
                    }

                    break;

                case UserMetadataFieldType.Boolean:
                    if (root is not { ValueKind: JsonValueKind.True or JsonValueKind.False })
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeBoolean)];
                        return false;
                    }

                    break;

                case UserMetadataFieldType.Select:
                    if (root.ValueKind != JsonValueKind.String)
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeString)];
                        return false;
                    }

                    string selected = root.GetString() ?? "";
                    if (field.Options != null && !field.Options.Contains(selected))
                    {
                        error = localizer[nameof(Resources.Program.Model_InvalidOptionSelected)];
                        return false;
                    }

                    break;

                case UserMetadataFieldType.MultiSelect:
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeArray)];
                        return false;
                    }

                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            error = localizer[nameof(Resources.Program.Model_ArrayItemsMustBeStrings)];
                            return false;
                        }

                        string itemStr = item.GetString() ?? "";
                        if (field.Options == null || field.Options.Contains(itemStr))
                            continue;

                        error = localizer[nameof(Resources.Program.Model_InvalidOptionSelectedWithValue), itemStr];
                        return false;
                    }

                    break;

                case UserMetadataFieldType.DateTime:
                case UserMetadataFieldType.Date:
                    if (root.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(root.GetString(), out _))
                    {
                        error = localizer[nameof(Resources.Program.Model_ValueMustBeDateTime)];
                        return false;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(field.Type));
            }

            return true;
        }
    }
}

public sealed record UserMetadataFieldValue(UserMetadataFieldValueType Type, JsonDocument? Value) : IDisposable
{
    public static readonly UserMetadataFieldValue Null = new(UserMetadataFieldValueType.Null, null);

    public override string ToString()
    {
        try
        {
            if (Value is null)
                return string.Empty;

            var root = Value.RootElement;
            return root.ValueKind switch
            {
                JsonValueKind.String => root.GetString() ?? string.Empty,
                JsonValueKind.Number => root.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Array => string.Join(", ",
                    root.EnumerateArray().Select(e => e.GetString() ?? string.Empty)),
                _ => string.Empty,
            };
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public void Dispose() => Value?.Dispose();
}
