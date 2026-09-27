# Legacy Content Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate legacy database content and legacy game ZIP packages into the canonical challenge library and draft learning routes without duplicating records or losing challenge behavior.

**Architecture:** Both sources map into an internal `CanonicalChallengeImport` graph. Source adapters validate and stage data, a transactional importer creates canonical rows and mappings, and a parity report compares every behavior bearing field. Startup executes resumable database batches before readiness; ZIP import uses staged blobs and compensation cleanup.

**Tech Stack:** ASP.NET Core 10 hosted services, EF Core 10, PostgreSQL, existing transfer models, existing storage abstraction, SHA-256, xUnit, Testcontainers.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Features/Imports/Application/CanonicalChallengeImport.cs` | Source neutral import graph and validation result. |
| `src/GZCTF/Features/Imports/Application/CanonicalImportService.cs` | Transactional challenge, route, module, mapping, and report creation. |
| `src/GZCTF/Features/Imports/Infrastructure/LegacyDatabaseSource.cs` | Read old GameChallenge and ExerciseChallenge data without mutating old tables. |
| `src/GZCTF/Features/Imports/Infrastructure/LegacyZipSource.cs` | Validate and map Manifest, TransferGame, TransferChallenge, and files. |
| `src/GZCTF/Features/Imports/Infrastructure/ImportBlobStaging.cs` | Stage, confirm, and compensate attachment writes. |
| `src/GZCTF/Features/Imports/Application/StartupLegacyMigrationService.cs` | Resume database backfill before readiness. |
| `src/GZCTF/Features/Imports/Application/ImportParityService.cs` | Produce machine readable per field parity reports. |
| `src/GZCTF/Features/Imports/Api/ImportsController.cs` | Administrator upload, status, report, and retry API. |
| `src/GZCTF/Extensions/Startup/DatabaseExtension.cs` | Run schema migration then legacy backfill before accepting traffic. |
| `src/GZCTF/Services/HealthCheck/MigrationHealthCheck.cs` | Fail readiness for running or unresolved failed startup batches. |
| `src/GZCTF.Integration.Test/Fixtures/Legacy/legacy-database.sql` | Deterministic old schema fixture with all four modes and account roles. |
| `src/GZCTF.Integration.Test/Fixtures/Legacy/legacy-game.zip` | Golden transfer package with all four modes and attachments. |
| `src/GZCTF.Integration.Test/Tests/Imports/LegacyDatabaseMigrationTests.cs` | Account, content, route, role, and idempotency tests. |
| `src/GZCTF.Integration.Test/Tests/Imports/LegacyZipImportTests.cs` | ZIP security, parity, idempotency, and rollback tests. |

## Task 1: Create deterministic golden legacy fixtures

- [ ] Add `legacy-database.sql` containing:
  - one Admin, one User, one Monitor, and one Banned account;
  - two games sharing a title but different identifiers;
  - static attachment, dynamic attachment, static container, and dynamic container questions;
  - repeated question titles, hints, flags, flag templates, attachments, and full container limits;
  - teams, participations, submissions, scores, and ranking history that must not become learning progress.
- [ ] Create `legacy-game.zip` using the repository's current export format. Include the same four challenge modes, Unicode paths, and known SHA-256 attachment hashes.
- [ ] Add `src/GZCTF.Integration.Test/Fixtures/Legacy/expected-parity.json` with exact source identifiers, field values, file hashes, and expected draft path/module ordering.
- [ ] Add a failing fixture integrity test in `LegacyDatabaseMigrationTests.cs` that loads the SQL and verifies the required source records exist before migration.
- [ ] Add a failing package integrity test in `LegacyZipImportTests.cs` that reads the package through existing transfer models and compares its fingerprint to `expected-parity.json`.
- [ ] Commit:

  ```bash
  git add src/GZCTF.Integration.Test/Fixtures/Legacy src/GZCTF.Integration.Test/Tests/Imports
  git commit -m "test: add legacy migration golden fixtures"
  ```

## Task 2: Define and validate the canonical import graph

- [ ] Add failing unit tests in `src/GZCTF.Test/UnitTests/Features/Imports/CanonicalChallengeImportTests.cs` for each challenge type, invalid mixed configurations, missing attachments, unsafe file paths, unsupported locale fallback, and duplicate source identifiers within a batch.
- [ ] Implement `CanonicalChallengeImport.cs` with immutable records for batch, path, module, challenge, localized content, hint, flag, attachment, and container configuration.
- [ ] Keep competition scoring, blood, team, and ranking values only in `LegacyMetadata`; do not expose them as canonical runtime properties.
- [ ] Map old dynamic scoring `Difficulty` into metadata and set canonical learning difficulty to `Normal`.
- [ ] Normalize any unsupported old locale to English fallback while retaining original text and locale code in metadata.
- [ ] Run the unit tests and commit:

  ```bash
  git add src/GZCTF/Features/Imports/Application/CanonicalChallengeImport.cs \
    src/GZCTF.Test/UnitTests/Features/Imports/CanonicalChallengeImportTests.cs
  git commit -m "feat: define canonical challenge import graph"
  ```

## Task 3: Implement the transactional canonical importer

- [ ] Add failing integration tests for one complete import, repeated source identifiers, two distinct challenges with identical content, and interruption after one module.
- [ ] Implement `CanonicalImportService.cs` so one batch creates every challenge independently, creates one draft path per source game, groups modules by old category, and preserves source order by old challenge ID.
- [ ] Insert `LegacyChallengeMap` and `LegacyPathMap` in the same transaction as their target rows. Use `(SourceKind, SourceScope, SourceId)` as the idempotency key.
- [ ] When a completed mapping exists, return the existing batch result. When an incomplete batch exists, resume only unmapped records and finalize the same batch.
- [ ] Create a single “Legacy Exercises” draft path when old exercise records exist. Use dependency order as recommendation order and create no lock relation.
- [ ] Leave official WP empty and add a warning per challenge. Preserve ordered old hints.
- [ ] Run importer tests. Expected: equal titles remain separate, retry counts are stable, and every source ID maps once.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Imports/Application/CanonicalImportService.cs \
    src/GZCTF.Integration.Test/Tests/Imports
  git commit -m "feat: add idempotent canonical importer"
  ```

## Task 4: Implement existing database startup migration

- [ ] Add failing `LegacyDatabaseMigrationTests.cs` cases that restore the SQL fixture, start the app, wait for readiness, stop it, and start it again against the same database.
- [ ] Implement `LegacyDatabaseSource.cs` using `AsNoTracking` projections from old tables. Read but never update or delete Game, GameChallenge, ExerciseChallenge, Team, Participation, Submission, and ranking rows.
- [ ] Implement `StartupLegacyMigrationService.cs` to find or create the database source batch, import in bounded chunks, persist progress after each chunk, and complete only after parity succeeds.
- [ ] Run the service after EF schema migrations and before the application reports ready. Add `MigrationHealthCheck.cs` so an unresolved failure keeps readiness unhealthy with a batch identifier and stable error code.
- [ ] Preserve identity rows and privileges. Map `Admin` to administrator, `User` and `Monitor` to learner behavior, and keep `Banned` disabled. Create no enrollments, lesson progress, challenge progress, daily stats, or submissions.
- [ ] Assert on the second start:
  - canonical record counts are unchanged;
  - all legacy table row counts are unchanged;
  - all four account sign in states remain correct;
  - learning progress tables are empty.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Imports/Infrastructure/LegacyDatabaseSource.cs \
    src/GZCTF/Features/Imports/Application/StartupLegacyMigrationService.cs \
    src/GZCTF/Extensions/Startup/DatabaseExtension.cs \
    src/GZCTF/Services/HealthCheck/MigrationHealthCheck.cs \
    src/GZCTF.Integration.Test/Tests/Imports/LegacyDatabaseMigrationTests.cs
  git commit -m "feat: migrate legacy database on startup"
  ```

## Task 5: Implement secure legacy ZIP import

- [ ] Add failing cases for path traversal, absolute paths, duplicate archive entries, oversized expanded content, wrong hash, missing attachment, unsupported manifest, repeated upload, and simulated database failure after blob staging.
- [ ] Implement `LegacyZipSource.cs` by reusing current Manifest, TransferGame, and TransferChallenge deserialization. Validate all paths before extraction and stream files with configured compressed and expanded size limits.
- [ ] Compute package SHA-256 before import and use it as the ZIP batch idempotency key.
- [ ] Implement `ImportBlobStaging.cs`: write files under a batch scoped temporary prefix, verify hashes, create database references through `CanonicalImportService`, then promote staged objects. On any failure, delete only objects created by that batch.
- [ ] Never delete a preexisting blob during compensation. Record reused and newly created blob hashes separately in the report.
- [ ] Implement administrator actions in `ImportsController.cs` for upload, batch status, report download, and retry of a failed batch.
- [ ] Apply existing administrator authorization and import upload rate limits.
- [ ] Run `LegacyZipImportTests`. Expected: valid import matches the golden report; a repeated upload creates nothing; every unsafe package fails without committed rows or leaked staged blobs.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Imports/Infrastructure/LegacyZipSource.cs \
    src/GZCTF/Features/Imports/Infrastructure/ImportBlobStaging.cs \
    src/GZCTF/Features/Imports/Api/ImportsController.cs \
    src/GZCTF.Integration.Test/Tests/Imports/LegacyZipImportTests.cs
  git commit -m "feat: import legacy game packages"
  ```

## Task 6: Produce and enforce parity reports

- [ ] Add failing tests that deliberately alter an imported flag, attachment hash, port, CPU limit, and hint order and assert each mismatch appears in the report.
- [ ] Implement `ImportParityService.cs` with per challenge comparisons for type, localized content, hints, flag rule or template, attachment name and hash, image, exposed port, CPU, memory, storage, and network mode.
- [ ] Include counts for games, draft paths, modules, each challenge type, flags, attachments, container configurations, warnings, and unsupported source fields.
- [ ] Block batch completion when a behavior bearing field differs. Allow missing WP and translated locale as explicit review warnings.
- [ ] Store the structured report on `MigrationBatch` and return it from the administrator API.
- [ ] Run both golden fixture suites and commit:

  ```bash
  git add src/GZCTF/Features/Imports/Application/ImportParityService.cs \
    src/GZCTF.Integration.Test/Tests/Imports
  git commit -m "feat: verify legacy import parity"
  ```

## Wave 3 verification

- [ ] Restore the database fixture and start twice. Save the two reports and diff their target identifiers and counts; expected: no difference.
- [ ] Import the ZIP fixture twice. Expected: the second request returns the existing completed batch identifier.
- [ ] Run migrated versions of all four challenges through the Wave 2 HTTP contract tests.
- [ ] Compare fixture attachment SHA-256 values with storage values after both database and ZIP migration.
- [ ] Verify old teams, competition submissions, scores, and rankings are absent from new progress queries while remaining untouched in old tables.
- [ ] Verify a migrated route stays draft and is invisible from public route discovery until an administrator publishes it.
