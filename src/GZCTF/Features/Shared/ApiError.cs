using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GZCTF.Features.Shared;

public sealed record ApiError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("traceId")] string TraceId,
    [property: JsonPropertyName("errors")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApiError Conflict(string code, string message, string traceId) =>
        new(code, message, traceId);

    public static ApiError Validation(
        string code,
        string message,
        string traceId,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(code, message, traceId, errors);
}
