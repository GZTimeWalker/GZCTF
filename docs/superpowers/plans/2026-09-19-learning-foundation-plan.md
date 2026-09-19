# Learning Domain Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the canonical data model and the smallest public and administrator APIs needed to create, publish, and preview learning routes without changing the existing container startup contract.

**Architecture:** Add domain focused folders under `Features` and map their entities explicitly from `AppDbContext`. Public reads project immutable published revisions. Administrator commands edit a single draft and publish it in a transaction guarded by a concurrency token. Existing competition tables continue to exist during the transition.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, Npgsql, xUnit, Testcontainers, NSwag/OpenAPI.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Extensions/Startup/DatabaseExtension.cs` | Redact database connection diagnostics. |
| `src/GZCTF/Features/Shared/ApiError.cs` | Stable error envelope and error codes. |
| `src/GZCTF/Features/ChallengeLibrary/Domain/ChallengeModels.cs` | Canonical challenge, localization, flags, hints, and WP records. |
| `src/GZCTF/Features/LearningPaths/Domain/LearningPathModels.cs` | Path, revision, module, item, lesson, enrollment, and lesson progress records. |
| `src/GZCTF/Features/Dashboard/Domain/DashboardModels.cs` | Cohort, dashboard, token, and daily read model records. |
| `src/GZCTF/Features/Imports/Domain/ImportModels.cs` | Batch and legacy source mappings. |
| `src/GZCTF/Features/LearningPaths/Infrastructure/LearningModelConfiguration.cs` | EF relationships, indexes, checks, concurrency, and delete behavior. |
| `src/GZCTF/Models/AppDbContext.cs` | DbSets and configuration registration. |
| `src/GZCTF/Migrations/20260919000100_AddLearningFoundation.cs` | Additive learning schema. |
| `src/GZCTF/Migrations/20260919000100_AddLearningFoundation.Designer.cs` | Generated migration metadata. |
| `src/GZCTF/Migrations/AppDbContextModelSnapshot.cs` | Current EF schema snapshot. |
| `src/GZCTF/Features/LearningPaths/Application/LearningPathService.cs` | Draft creation, editing, publication, preview projection. |
| `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs` | Administrator challenge and lesson commands. |
| `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeMergeService.cs` | Manual duplicate merge with reference and progress reconciliation. |
| `src/GZCTF/Features/ChallengeLibrary/Api/AdminChallengesController.cs` | Administrator challenge library API. |
| `src/GZCTF/Features/ChallengeLibrary/Api/AdminLessonsController.cs` | Administrator Markdown lesson API. |
| `src/GZCTF/Features/LearningPaths/Api/LearningPathsController.cs` | Anonymous route list and preview endpoints. |
| `src/GZCTF/Features/LearningPaths/Api/AdminLearningPathsController.cs` | Administrator draft and publish endpoints. |
| `src/GZCTF/Features/LearningPaths/Api/EnrollmentsController.cs` | Join, leave, and select the current path. |
| `src/GZCTF/Features/LearningPaths/Api/LessonsController.cs` | Protected lesson content and completion. |
| `src/GZCTF/Features/LearningProgress/Application/LearningRecordService.cs` | Aggregate current revision progress and recent activity. |
| `src/GZCTF/Features/LearningProgress/Api/MyLearningController.cs` | Personal learning record API. |
| `src/GZCTF/Extensions/Startup/ServicesExtension.cs` | Register application services. |
| `src/GZCTF.Test/UnitTests/Features/Shared/ApiErrorTests.cs` | Error contract tests. |
| `src/GZCTF.Integration.Test/Tests/Learning/LearningSchemaTests.cs` | Constraints and migration tests. |
| `src/GZCTF.Integration.Test/Tests/Learning/LearningPathPublishingTests.cs` | Publication, immutability, and preview access tests. |

## Task 1: Establish a repeatable backend baseline and redact secrets

- [ ] Add a failing unit test in `src/GZCTF.Test/UnitTests/Features/Shared/ApiErrorTests.cs` that serializes `ApiError` and asserts the fields are exactly `code`, `message`, `traceId`, and optional `errors`.
- [ ] Add `src/GZCTF/Features/Shared/ApiError.cs` with an immutable response record and a small factory for validation and conflict responses.
- [ ] Add a testable connection diagnostic helper in `src/GZCTF/Extensions/Startup/DatabaseExtension.cs`; its result may include host and database but must exclude password, username, and raw connection string.
- [ ] Add a unit test in `src/GZCTF.Test/UnitTests/Features/Shared/DatabaseDiagnosticTests.cs` using `Host=db;Database=gzctf;Username=admin;Password=secret` and assert neither `admin` nor `secret` is present.
- [ ] Replace the current full connection string log argument with the redacted diagnostic.
- [ ] Run:

  ```bash
  docker run --rm -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet test src/GZCTF.Test/GZCTF.Test.csproj --filter 'ApiErrorTests|DatabaseDiagnosticTests'
  ```

  Expected: both test classes pass and the output contains no credential value.

- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Shared src/GZCTF/Extensions/Startup/DatabaseExtension.cs src/GZCTF.Test/UnitTests/Features/Shared
  git commit -m "fix: redact database connection diagnostics"
  ```

## Task 2: Add the canonical challenge and learning entities

- [ ] Create `src/GZCTF.Integration.Test/Tests/Learning/LearningSchemaTests.cs` with failing tests for these invariants:
  - `LearningPath.Slug` is unique.
  - `(ChallengeId, Locale)`, `(LessonId, Locale)`, and `(ModuleId, Locale)` are unique.
  - A `ModuleItem` references exactly one of `LessonId` and `ChallengeId`.
  - `(UserId, PathId)`, `(UserId, LessonId)`, and `(UserId, ChallengeId)` are unique.
  - Each path has at most one draft revision and one current published revision.
  - A user has at most one current enrollment.
- [ ] Implement canonical challenge records in `src/GZCTF/Features/ChallengeLibrary/Domain/ChallengeModels.cs`. Use explicit enums for publication state, difficulty, and the existing four challenge types. Keep flag material in `ChallengeFlag`, separate from public challenge fields.
- [ ] Implement path and lesson records in `src/GZCTF/Features/LearningPaths/Domain/LearningPathModels.cs`. Model `ModuleItem` with nullable `LessonId` and `ChallengeId` plus a database check constraint enforcing exclusive ownership.
- [ ] Add `SourceType`, `SourceId`, `SourceName`, and JSON metadata fields to `Challenge` for audit without making legacy identifiers the primary key.
- [ ] Add `RowVersion` concurrency columns to `LearningPath` and `LearningPathRevision`.
- [ ] Register the new DbSets in `src/GZCTF/Models/AppDbContext.cs` and call `LearningModelConfiguration.Configure(modelBuilder)` from `OnModelCreating`.
- [ ] Implement `src/GZCTF/Features/LearningPaths/Infrastructure/LearningModelConfiguration.cs` with explicit indexes and `DeleteBehavior.Restrict` for reusable content.
- [ ] Generate and normalize the migration:

  ```bash
  docker run --rm -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet ef migrations add AddLearningFoundation \
      --project src/GZCTF/GZCTF.csproj \
      --output-dir Migrations
  ```

  Rename the generated pair to `20260919000100_AddLearningFoundation.cs` and `20260919000100_AddLearningFoundation.Designer.cs`, and set the migration identifier to the same timestamp so the checked in path is deterministic.
- [ ] Run the schema tests using the repository's Testcontainers fixture:

  ```bash
  dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj \
    --filter FullyQualifiedName~LearningSchemaTests
  ```

  Expected: every uniqueness and check constraint test passes against PostgreSQL.

- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeLibrary src/GZCTF/Features/LearningPaths/Domain \
    src/GZCTF/Features/LearningPaths/Infrastructure src/GZCTF/Models/AppDbContext.cs \
    src/GZCTF/Migrations src/GZCTF.Integration.Test/Tests/Learning/LearningSchemaTests.cs
  git commit -m "feat: add canonical learning domain schema"
  ```

## Task 3: Add cohorts, dashboards, and import bookkeeping to the same additive schema

- [ ] Extend `LearningSchemaTests.cs` with failing assertions for unique cohort names, unique `(UserId, Date)` daily stats, unique token hashes, and unique legacy source mappings.
- [ ] Add `Cohort`, `Dashboard`, `DashboardToken`, and `LearnerDailySolveStat` to `src/GZCTF/Features/Dashboard/Domain/DashboardModels.cs`.
- [ ] Add nullable `CohortId` to the existing user information entity and map it with `DeleteBehavior.SetNull`.
- [ ] Add `MigrationBatch`, `LegacyChallengeMap`, and `LegacyPathMap` to `src/GZCTF/Features/Imports/Domain/ImportModels.cs`. Store batch state as a constrained enum and the SHA-256 package fingerprint as a unique value.
- [ ] Extend the foundation migration rather than creating a second migration because Wave 1 has not shipped.
- [ ] Run `LearningSchemaTests` again and verify all constraints pass.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Dashboard src/GZCTF/Features/Imports \
    src/GZCTF/Models src/GZCTF/Migrations src/GZCTF.Integration.Test/Tests/Learning/LearningSchemaTests.cs
  git commit -m "feat: add learning analytics and import schema"
  ```

## Task 4: Implement administrator challenge and lesson libraries

- [ ] Add failing integration tests in `src/GZCTF.Integration.Test/Tests/Learning/ChallengeLibraryTests.cs` for administrator only challenge and lesson CRUD, immutable challenge type after first publication, localized content fallback, secret flag exclusion, and Markdown lesson retrieval.
- [ ] Implement `ChallengeLibraryService.cs` with explicit command records for metadata, four mode runtime configuration, localized content, flags, attachments, hints, and WP. Return flags only from a dedicated administrator edit response and mark that response as noncacheable.
- [ ] Implement `AdminChallengesController.cs` and `AdminLessonsController.cs` as small administrator protected endpoints. Keep attachment writes behind the existing blob abstraction and validate runtime configuration with the same canonical rules used by imports.
- [ ] Add failing merge cases to `ChallengeLibraryTests.cs`: two imported duplicates stay separate before a manual command; merge redirects every `ModuleItem`; earliest solve per user is retained; duplicate progress and daily stats do not double count; source mappings for both records remain auditable.
- [ ] Implement `ChallengeMergeService.cs` as one transaction. Require an administrator to choose the surviving challenge and runtime resources, reject an active instance conflict, redirect module references, reconcile progress by earliest solve, rebuild affected daily stats, mark the losing challenge merged, and retain its source mappings.
- [ ] Run:

  ```bash
  dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj \
    --filter FullyQualifiedName~ChallengeLibraryTests
  ```

  Expected: library authorization, secret protection, localization, and merge reconciliation pass.

- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeLibrary src/GZCTF.Integration.Test/Tests/Learning/ChallengeLibraryTests.cs
  git commit -m "feat: add canonical content administration"
  ```

## Task 5: Implement route draft and atomic publication

- [ ] Add failing integration cases to `src/GZCTF.Integration.Test/Tests/Learning/LearningPathPublishingTests.cs`:
  - An administrator can create a path with one draft.
  - A second draft request returns the existing draft.
  - Publishing rejects an empty module, an unpublished challenge, or missing English fallback text.
  - Publishing switches `CurrentPublishedRevisionId` inside one transaction.
  - A stale `RowVersion` returns HTTP `409` with code `learning.revision_conflict`.
  - Editing after publish creates a new draft and leaves the published graph unchanged.
- [ ] Implement request and response records alongside `LearningPathService.cs`; include stable identifiers, localized strings, sort order, expected minutes, counts, and row version. Exclude Markdown lesson bodies and challenge bodies from preview DTOs.
- [ ] Implement `LearningPathService` with explicit projections and transactions. Copy a published revision to a draft by creating new module and item rows that continue to reference stable `Lesson` and `Challenge` rows.
- [ ] Publish by locking the path row, rechecking the row version, validating every referenced content item, marking the draft published, and switching the pointer before commit.
- [ ] In `LearningPathsController.cs`, expose:

  ```text
  GET /api/learning-paths
  GET /api/learning-paths/{slug}/preview
  ```

- [ ] In `AdminLearningPathsController.cs`, expose administrator protected create, get draft, edit draft, preview draft, and publish actions. Reuse the existing privilege middleware and require `Admin`.
- [ ] Register `LearningPathService` in `ServicesExtension.cs`.
- [ ] Run:

  ```bash
  dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj \
    --filter FullyQualifiedName~LearningPathPublishingTests
  ```

  Expected: anonymous preview, immutable publication, and concurrency tests pass.

- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/LearningPaths src/GZCTF/Extensions/Startup/ServicesExtension.cs \
    src/GZCTF.Integration.Test/Tests/Learning/LearningPathPublishingTests.cs
  git commit -m "feat: publish immutable learning paths"
  ```

## Task 6: Add enrollment and lesson completion application services

- [ ] Add failing tests in `src/GZCTF.Integration.Test/Tests/Learning/EnrollmentTests.cs` for joining multiple paths, selecting one current path, leaving the current path, reading a lesson as an enrolled learner, and idempotently completing a lesson.
- [ ] Add `EnrollmentService.cs` and `LessonProgressService.cs` under `src/GZCTF/Features/LearningPaths/Application`.
- [ ] Change the current enrollment in one transaction: clear any prior current row and set the selected enrollment current.
- [ ] Return lesson Markdown only to authenticated users. Return `404` for content absent from the current published revision and `403` for anonymous users.
- [ ] Add learner endpoints under `src/GZCTF/Features/LearningPaths/Api/EnrollmentsController.cs` and `LessonsController.cs`.
- [ ] Run `EnrollmentTests`; expect all access and idempotency cases to pass.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/LearningPaths src/GZCTF.Integration.Test/Tests/Learning/EnrollmentTests.cs
  git commit -m "feat: add enrollment and lesson progress"
  ```

## Task 7: Add personal learning record projections

- [ ] Add failing tests in `src/GZCTF.Integration.Test/Tests/Learning/LearningRecordTests.cs` for route percentage, completed module count, completed lesson count, globally unique solved challenge count, recent lesson and challenge activity, help based solve mode, and a challenge reused by two routes.
- [ ] Implement `LearningRecordService.cs` using the current published revision as each route denominator. Count a module complete only when every current item is complete; count lesson and challenge items as one hour each; count a reused challenge once in global solved total and in every route that references it.
- [ ] Return joined routes, current route identity, per route and per module aggregates, completed lesson identifiers, challenge solve modes, and a bounded recent activity list from `MyLearningController.cs`.
- [ ] Keep aggregate values out of public and route outline DTOs. Add serialization tests that fail if `progressPercent`, `completedModules`, or `completedLessons` appears in those responses.
- [ ] Run `LearningRecordTests`; expected: aggregates reflect the current revision and existing completion rows survive a newly published revision.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/LearningProgress src/GZCTF.Integration.Test/Tests/Learning/LearningRecordTests.cs
  git commit -m "feat: add personal learning records"
  ```

## Wave 1 verification

- [ ] Run all new learning tests and the existing unit suite.
- [ ] Create a fresh PostgreSQL database and apply all migrations from zero.
- [ ] Apply migrations to a copy of the legacy fixture and verify no old table is dropped.
- [ ] Fetch a published route preview anonymously and verify the payload has counts and titles but no lesson body, challenge body, flag, attachment URL, or aggregate learner progress.
- [ ] Review the generated SQL for route listing and preview; assert a bounded number of queries and no `AutoInclude` joins.
- [ ] Record the command output in the change review before starting Wave 2.
