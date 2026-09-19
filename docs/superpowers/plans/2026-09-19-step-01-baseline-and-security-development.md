# Step 01 Baseline and Security Development Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish a reproducible .NET 10 unit test baseline, introduce the stable error envelope used by new Learning APIs, and stop database usernames, passwords, raw connection strings, and raw configuration exception messages from entering startup logs.

**Architecture:** Add two small dependency free shared types. `ApiError` defines the serialized contract for new feature APIs and is registered with the existing source generated JSON context. `DatabaseConnectionDiagnostic` parses the PostgreSQL connection string but retains only host and database. `DatabaseExtension` logs those safe fields plus the exception type and preserves the existing startup failure behavior.

**Tech Stack:** .NET SDK 10.0.401, ASP.NET Core 10, System.Text.Json source generation, Npgsql, Serilog, xUnit, Docker.

---

## 1. Dispatch metadata

| Field | Value |
|---|---|
| Task ID | `S01` |
| Queue | Wave 1 — Foundation |
| Owner role | `FOUNDATION` |
| Dependencies | Approved design, roadmap, and task dispatch plan |
| Unblocks | `S02` canonical domain entities |
| Expected commit | `fix: establish safe learning API foundation` |
| Product behavior change | Startup failure diagnostics are redacted; no endpoint or database behavior changes |

## 2. Current evidence

The current leak is in `src/GZCTF/Extensions/Startup/DatabaseExtension.cs`:

```csharp
Log.Logger.Error(StaticLocalizer[
    nameof(Resources.Program.Database_CurrentConnectionString),
    builder.Configuration.GetConnectionString("Database") ?? "null"]);
```

The same failure branch passes `e.Message` into the fatal message. Provider and parser exception messages are not a safe logging boundary because they may include configuration fragments.

The repository already permits `GZCTF.Test` to access internal application types through `InternalsVisibleTo`, so no production visibility needs to be widened for these tests.

The actual solution file is `src/GZCTF.slnx`, and the pinned SDK is `10.0.401` in `src/global.json`.

## 3. Scope

### Files to create

- `src/GZCTF/Features/Shared/ApiError.cs`
- `src/GZCTF/Extensions/Startup/DatabaseConnectionDiagnostic.cs`
- `src/GZCTF.Test/UnitTests/Features/Shared/ApiErrorTests.cs`
- `src/GZCTF.Test/UnitTests/Features/Shared/DatabaseConnectionDiagnosticTests.cs`

### Files to modify

- `src/GZCTF/Utils/JsonSerializerContext.cs`
- `src/GZCTF/Extensions/Startup/DatabaseExtension.cs`

### Files outside scope

- Existing `RequestResponse` responses and legacy controllers.
- Database schema, EF migrations, and PostgreSQL data.
- Dockerfile, ports, health checks, storage, Redis, and container providers.
- Frontend error rendering. It begins when the first Learning endpoint consumes `ApiError`.

## 4. Acceptance criteria

- [ ] The prechange unit test command is recorded and exits `0`.
- [ ] `ApiError` serializes exactly `code`, `message`, `traceId`, and optional `errors`.
- [ ] A conflict factory does not include `errors`; a validation factory includes field error arrays.
- [ ] The JSON source generation context includes `ApiError`.
- [ ] A valid PostgreSQL connection string produces only host and database diagnostic fields.
- [ ] Missing or malformed connection strings produce fixed safe values and never echo the input.
- [ ] `DatabaseExtension` never logs the raw database connection string.
- [ ] `DatabaseExtension` never logs `e.Message` or the full exception object in the configuration failure branch.
- [ ] Database configuration still calls `UseNpgsql` with the original unmodified connection string.
- [ ] Configuration failure still terminates through `ExitWithFatalMessage`.
- [ ] Focused and complete unit tests pass in .NET 10.
- [ ] `git diff --check` passes.

## 5. Step A — Capture the baseline

- [ ] Confirm the worktree contains only expected prior documentation commits:

  ```bash
  git status --short --branch
  git log -3 --oneline
  ```

  Expected: no uncommitted source change. Documentation ignored by repository rules must already be force added and committed.

- [ ] Confirm frontend static checks still pass before backend changes:

  ```bash
  (cd src/GZCTF/ClientApp && pnpm check)
  ```

  Expected: exit `0` with no TypeScript errors.

- [ ] Run the current unit project in the pinned SDK container:

  ```bash
  docker run --rm \
    -v "$PWD:/workspace" \
    -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet test src/GZCTF.Test/GZCTF.Test.csproj \
      --configuration Release \
      --logger "console;verbosity=normal"
  ```

  Expected: restore succeeds, the existing test count is recorded in the handoff, and all existing unit tests pass. If a preexisting test fails, record the exact test and stop S01 source edits until the baseline failure is understood.

## 6. Step B — Write failing tests for the API error contract

- [ ] Create `src/GZCTF.Test/UnitTests/Features/Shared/ApiErrorTests.cs` with this content:

  ```csharp
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
  ```

- [ ] Run the focused test and confirm it fails because `GZCTF.Features.Shared.ApiError` does not exist:

  ```bash
  docker run --rm -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet test src/GZCTF.Test/GZCTF.Test.csproj \
      --configuration Release \
      --filter FullyQualifiedName~ApiErrorTests
  ```

  Expected failure: compiler errors identify the missing `GZCTF.Features.Shared` namespace or `ApiError` type. A different failure must be resolved before implementation.

## 7. Step C — Implement the API error contract

- [ ] Create `src/GZCTF/Features/Shared/ApiError.cs`:

  ```csharp
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
  ```

- [ ] Modify `src/GZCTF/Utils/JsonSerializerContext.cs`:
  - add `using GZCTF.Features.Shared;` with the existing application usings;
  - add `[JsonSerializable(typeof(ApiError))]` beside the primitive and response registrations.

- [ ] Rerun the focused test.

  Expected: both `ApiErrorTests` pass. Do not change existing `RequestResponse` behavior in S01.

## 8. Step D — Write failing tests for safe database diagnostics

- [ ] Create `src/GZCTF.Test/UnitTests/Features/Shared/DatabaseConnectionDiagnosticTests.cs`:

  ```csharp
  using System;
  using GZCTF.Extensions.Startup;
  using Xunit;

  namespace GZCTF.Test.UnitTests.Features.Shared;

  public class DatabaseConnectionDiagnosticTests
  {
      [Fact]
      public void Parse_ValidConnectionString_KeepsOnlyHostAndDatabase()
      {
          const string connectionString =
              "Host=postgres.internal;Port=5432;Database=gzctf;" +
              "Username=admin;Password=secret;Application Name=learning-platform";

          var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

          Assert.Equal("postgres.internal", diagnostic.Host);
          Assert.Equal("gzctf", diagnostic.Database);
          Assert.DoesNotContain("admin", diagnostic.LogValue, StringComparison.Ordinal);
          Assert.DoesNotContain("secret", diagnostic.LogValue, StringComparison.Ordinal);
          Assert.DoesNotContain("learning-platform", diagnostic.LogValue, StringComparison.Ordinal);
          Assert.DoesNotContain("5432", diagnostic.LogValue, StringComparison.Ordinal);
      }

      [Theory]
      [InlineData(null)]
      [InlineData("")]
      [InlineData("Password=secret;this is not valid")]
      public void Parse_MissingOrMalformedConnectionString_DoesNotEchoInput(string? connectionString)
      {
          var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

          Assert.DoesNotContain("secret", diagnostic.LogValue, StringComparison.OrdinalIgnoreCase);
          Assert.DoesNotContain("this is not valid", diagnostic.LogValue,
              StringComparison.OrdinalIgnoreCase);
      }

      [Fact]
      public void Parse_ControlCharacters_RemovesLogInjectionCharacters()
      {
          const string connectionString =
              "Host=postgres\nforged;Database=gzctf\rfake;Username=user;Password=secret";

          var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);

          Assert.DoesNotContain('\n', diagnostic.LogValue);
          Assert.DoesNotContain('\r', diagnostic.LogValue);
      }
  }
  ```

- [ ] Run only `DatabaseConnectionDiagnosticTests`.

  Expected failure: the compiler cannot find `DatabaseConnectionDiagnostic`.

## 9. Step E — Implement safe database diagnostics

- [ ] Create `src/GZCTF/Extensions/Startup/DatabaseConnectionDiagnostic.cs`:

  ```csharp
  using Npgsql;

  namespace GZCTF.Extensions.Startup;

  internal sealed record DatabaseConnectionDiagnostic(string Host, string Database)
  {
      internal string LogValue => $"Host={Host};Database={Database}";

      internal static DatabaseConnectionDiagnostic Parse(string? connectionString)
      {
          if (string.IsNullOrWhiteSpace(connectionString))
              return new("unknown", "unknown");

          try
          {
              var builder = new NpgsqlConnectionStringBuilder(connectionString);
              return new(Sanitize(builder.Host), Sanitize(builder.Database));
          }
          catch (ArgumentException)
          {
              return new("invalid", "unknown");
          }
      }

      private static string Sanitize(string? value) =>
          string.IsNullOrWhiteSpace(value)
              ? "unknown"
              : value.Replace('\r', ' ').Replace('\n', ' ');
  }
  ```

  `Parse` must never return username, password, port, options, or the original malformed value. Catch only the parser's expected `ArgumentException`; unexpected programming errors should not be hidden.

- [ ] Refactor `ConfigureDatabase` in `src/GZCTF/Extensions/Startup/DatabaseExtension.cs` to read the connection string once and pass the original value unchanged to both Npgsql registrations:

  ```csharp
  var connectionString = builder.Configuration.GetConnectionString("Database");

  builder.Services.AddDbContext<AppDbContext>(options =>
  {
      options.UseNpgsql(connectionString,
          o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

      if (!builder.Environment.IsDevelopment())
          return;

      options.EnableSensitiveDataLogging();
      options.EnableDetailedErrors();
  });
  ```

- [ ] Replace the current `catch` body with a safe structured diagnostic:

  ```csharp
  catch (Exception exception)
  {
      var diagnostic = DatabaseConnectionDiagnostic.Parse(connectionString);
      Log.Logger.Error(
          "Database configuration failed for {DatabaseHost}/{DatabaseName} ({FailureType})",
          diagnostic.Host,
          diagnostic.Database,
          exception.GetType().Name);

      ExitWithFatalMessage(
          StaticLocalizer[nameof(Resources.Program.Database_ConnectionFailed),
              exception.GetType().Name]);
  }
  ```

  Do not pass `exception`, `exception.Message`, or `connectionString` to Serilog. Keep `ExitWithFatalMessage` so the startup failure mode remains unchanged.

- [ ] Run the diagnostic tests.

  Expected: all valid, malformed, and control character cases pass.

## 10. Step F — Verify security and unchanged behavior

- [ ] Run both new test classes:

  ```bash
  docker run --rm -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet test src/GZCTF.Test/GZCTF.Test.csproj \
      --configuration Release \
      --filter 'FullyQualifiedName~ApiErrorTests|FullyQualifiedName~DatabaseConnectionDiagnosticTests'
  ```

  Expected: all S01 tests pass.

- [ ] Run the complete unit project using the baseline command from Step A.

  Expected: the previous passing count plus the new S01 cases pass.

- [ ] Run source scans:

  ```bash
  rg -n 'Database_CurrentConnectionString|GetConnectionString\("Database"\).*Log|exception\.Message|e\.Message' \
    src/GZCTF/Extensions/Startup/DatabaseExtension.cs
  rg -n 'Password|Username|User ID|Pwd' \
    src/GZCTF/Extensions/Startup/DatabaseConnectionDiagnostic.cs
  ```

  Expected: both commands return no matches. The diagnostic test fixture may contain fake credentials; production files may not retain or log those fields.

- [ ] Review the exact production diff:

  ```bash
  git diff -- \
    src/GZCTF/Features/Shared/ApiError.cs \
    src/GZCTF/Utils/JsonSerializerContext.cs \
    src/GZCTF/Extensions/Startup/DatabaseConnectionDiagnostic.cs \
    src/GZCTF/Extensions/Startup/DatabaseExtension.cs
  git diff --check
  ```

- [ ] Confirm `src/GZCTF/Dockerfile`, `src/GZCTF/Server.cs`, configuration keys, ports, and database schema are absent from the diff.

## 11. Step G — Commit and hand off

- [ ] Stage only S01 files:

  ```bash
  git add \
    src/GZCTF/Features/Shared/ApiError.cs \
    src/GZCTF/Utils/JsonSerializerContext.cs \
    src/GZCTF/Extensions/Startup/DatabaseConnectionDiagnostic.cs \
    src/GZCTF/Extensions/Startup/DatabaseExtension.cs \
    src/GZCTF.Test/UnitTests/Features/Shared/ApiErrorTests.cs \
    src/GZCTF.Test/UnitTests/Features/Shared/DatabaseConnectionDiagnosticTests.cs
  git commit -m "fix: establish safe learning API foundation"
  ```

- [ ] Write the handoff with:
  - the created commit SHA;
  - the baseline and final unit test counts;
  - confirmation that both secret scans returned no matches;
  - confirmation that no runtime API, schema, Docker, or port changed;
  - `S02` as the next unblocked task.

## 12. Failure handling

- If the SDK image cannot be pulled, record the Docker error and use a host SDK only if `dotnet --version` reports `10.0.401` or a compatible `10.0.4xx` selected by `src/global.json`.
- If the prechange unit suite fails, do not fold unrelated repairs into S01. Diagnose the failure and create a separate prerequisite commit.
- If `NpgsqlConnectionStringBuilder` throws a different expected parse exception for a test case, narrow the fixture or catch the documented parser exception type. Do not add a broad catch that silently hides application errors.
- If a reviewer requests richer database diagnostics, add an allowlisted field to `DatabaseConnectionDiagnostic` with a test proving it cannot carry a credential. Never reintroduce the raw string or exception message.
- Revert S01 by reverting its single focused commit; no data rollback or migration is required.
