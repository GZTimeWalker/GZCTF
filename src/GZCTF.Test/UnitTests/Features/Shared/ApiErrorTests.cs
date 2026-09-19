using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GZCTF.Features.Shared;
using Xunit;

namespace GZCTF.Test.UnitTests.Features.Shared;

public class ApiErrorTests
{
    [Fact]
    public void Conflict_SerializesStableEnvelopeWithoutErrors()
    {
        var error = ApiError.Conflict(
            "learning.revision_conflict",
            "The learning path changed. Reload and try again.",
            "trace-123");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error));
        var root = document.RootElement;

        Assert.Equal("learning.revision_conflict", root.GetProperty("code").GetString());
        Assert.Equal("The learning path changed. Reload and try again.",
            root.GetProperty("message").GetString());
        Assert.Equal("trace-123", root.GetProperty("traceId").GetString());
        Assert.False(root.TryGetProperty("errors", out _));
        Assert.Equal(3, root.EnumerateObject().Count());
    }

    [Fact]
    public void Validation_SerializesFieldErrors()
    {
        IReadOnlyDictionary<string, string[]> errors =
            new Dictionary<string, string[]>
            {
                ["slug"] = ["Slug is required."],
                ["title"] = ["English title is required."]
            };

        var error = ApiError.Validation(
            "validation.failed",
            "The request is invalid.",
            "trace-456",
            errors);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error));
        var root = document.RootElement;

        Assert.Equal("Slug is required.", root.GetProperty("errors")
            .GetProperty("slug")[0].GetString());
        Assert.Equal("English title is required.", root.GetProperty("errors")
            .GetProperty("title")[0].GetString());
        Assert.Equal(4, root.EnumerateObject().Count());
    }
}
