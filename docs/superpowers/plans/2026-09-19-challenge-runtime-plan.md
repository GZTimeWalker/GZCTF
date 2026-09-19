# Challenge Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run all four legacy challenge types through one learner scoped runtime and record submissions, global completion, hints, WP views, and solve attribution.

**Architecture:** A `ChallengeRuntimeService` orchestrates small flag, attachment, and container services behind interfaces. User instances are created on first access, dynamic resources are allocated atomically, and `ChallengeProgress` is inserted once in the same transaction as the first accepted submission. Existing storage and container providers remain unchanged adapters.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL locking and unique constraints, existing storage providers, existing Docker/Kubernetes container managers, SignalR, xUnit, Testcontainers.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Features/ChallengeRuntime/Domain/ChallengeRuntimeModels.cs` | User instance, submission, help usage, and solve mode; global progress comes from the Foundation schema. |
| `src/GZCTF/Features/ChallengeRuntime/Application/ChallengeRuntimeService.cs` | Open, start, extend, stop, and inspect a learner instance. |
| `src/GZCTF/Features/ChallengeRuntime/Application/ChallengeSubmissionService.cs` | Validate flags and create first completion atomically. |
| `src/GZCTF/Features/ChallengeRuntime/Application/DynamicAttachmentAllocator.cs` | Transactional attachment claim and release. |
| `src/GZCTF/Features/ChallengeRuntime/Application/ChallengeHelpService.cs` | Ordered hints, WP, and usage audit. |
| `src/GZCTF/Features/ChallengeRuntime/Infrastructure/LegacyContainerRuntimeAdapter.cs` | Adapt canonical configuration to current container manager. |
| `src/GZCTF/Features/ChallengeRuntime/Infrastructure/LegacyStorageAdapter.cs` | Adapt canonical attachments to current storage interface. |
| `src/GZCTF/Features/ChallengeRuntime/Api/ChallengesController.cs` | Authenticated challenge detail, hint, WP, and attachment actions. |
| `src/GZCTF/Features/ChallengeRuntime/Api/ChallengeInstancesController.cs` | Instance lifecycle actions. |
| `src/GZCTF/Features/ChallengeRuntime/Api/ChallengeSubmissionsController.cs` | Flag submission and recent own submissions. |
| `src/GZCTF/Models/AppDbContext.cs` | Runtime DbSets and constraints. |
| `src/GZCTF/Migrations/20260919000200_AddChallengeRuntime.cs` | Runtime tables and indexes. |
| `src/GZCTF.Integration.Test/Tests/Runtime/ChallengeModeContractTests.cs` | Four mode behavior parity. |
| `src/GZCTF.Integration.Test/Tests/Runtime/ChallengeConcurrencyTests.cs` | Instance, attachment, and progress races. |
| `src/GZCTF.Integration.Test/Tests/Runtime/ChallengeHelpTests.cs` | Hint, WP, and attribution rules. |

## Task 1: Capture the four legacy mode contracts

- [ ] Add fixtures under `src/GZCTF.Integration.Test/Fixtures/Challenges/` for static attachment, dynamic attachment, static container, and dynamic container. Each fixture records title, type, flag rule, attachment hash or container image, exposed port, CPU, memory, storage, and network mode.
- [ ] Add failing theory cases in `ChallengeModeContractTests.cs` that state the expected behavior:
  - static attachment returns the same file and validates the configured static flag;
  - dynamic attachment gives each user one reserved file and validates that file's flag;
  - static container uses the configured global flag and canonical container limits;
  - dynamic container injects the user's generated flag and canonical container limits.
- [ ] Test that an anonymous request cannot read challenge content, download an attachment, start an instance, or submit a flag.
- [ ] Commit the failing characterization tests separately:

  ```bash
  git add src/GZCTF.Integration.Test/Fixtures/Challenges src/GZCTF.Integration.Test/Tests/Runtime/ChallengeModeContractTests.cs
  git commit -m "test: define challenge runtime contracts"
  ```

## Task 2: Add runtime persistence with explicit invariants

- [ ] Add `UserChallengeInstance`, `ChallengeSubmission`, and `ChallengeHelpUsage` in `ChallengeRuntimeModels.cs`. Reuse the Foundation `ChallengeProgress` entity rather than defining a second completion model.
- [ ] Use a status enum for instance lifecycle and solve mode values `Independent`, `AfterHint`, and `AfterWriteup`.
- [ ] Add a unique index for active `(UserId, ChallengeId)` instances and verify the existing unique `(UserId, ChallengeId)` progress index. Store accepted and rejected submissions, but never include the expected flag value.
- [ ] Store one help row per hint view and a single first WP view timestamp. Add indexes supporting user history and challenge analytics.
- [ ] Add runtime DbSets and explicit relationships to `AppDbContext`.
- [ ] Generate `20260919000200_AddChallengeRuntime.cs` and its designer file with `dotnet ef migrations add AddChallengeRuntime`, then normalize the timestamp and identifier.
- [ ] Add failing constraint tests to `ChallengeConcurrencyTests.cs`, then run them against PostgreSQL.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime/Domain src/GZCTF/Models/AppDbContext.cs \
    src/GZCTF/Migrations src/GZCTF.Integration.Test/Tests/Runtime/ChallengeConcurrencyTests.cs
  git commit -m "feat: add learner challenge runtime schema"
  ```

## Task 3: Implement static and dynamic attachment access

- [ ] Extend `ChallengeModeContractTests.cs` with failing authenticated download tests and a parallel allocation test with two learners and two dynamic files.
- [ ] Add `LegacyStorageAdapter.cs` that calls the existing storage interface and returns a short lived download response without exposing the storage key in list APIs.
- [ ] Add `DynamicAttachmentAllocator.cs`. In one PostgreSQL transaction, select an unclaimed attachment using row locking with `SKIP LOCKED`, attach it to the active user instance, and commit. If no file remains, return error code `challenge.dynamic_attachment_exhausted`.
- [ ] Make repeated open and download calls return the same assignment for the same active instance.
- [ ] Release a reservation only when an administrator retires the imported resource; normal user stop actions must not change the learner's assigned flag.
- [ ] Run attachment theory and concurrency tests. Expected: hashes match fixtures, users get different files, and repeated calls are stable.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime/Application/DynamicAttachmentAllocator.cs \
    src/GZCTF/Features/ChallengeRuntime/Infrastructure/LegacyStorageAdapter.cs \
    src/GZCTF.Integration.Test/Tests/Runtime
  git commit -m "feat: run static and dynamic attachment challenges"
  ```

## Task 4: Implement idempotent container lifecycle

- [ ] Extend `ChallengeModeContractTests.cs` with failing container adapter tests using the existing fake or mocked container manager. Assert exact image, port, CPU, memory, storage, network mode, and dynamic flag environment values.
- [ ] Add `LegacyContainerRuntimeAdapter.cs` that translates only canonical runtime fields to the existing container manager contract.
- [ ] Add `ChallengeRuntimeService.cs` with `GetOrCreateInstance`, `Start`, `Extend`, `Stop`, and `GetStatus` methods.
- [ ] Protect startup with the active instance unique constraint and an application transaction. If concurrent starts race, load and return the winning instance rather than creating a second container.
- [ ] Preserve current maximum lifetime and cleanup behavior through the adapter; use stable API error codes for capacity, image, and provider failures.
- [ ] Run the container theory and parallel start tests. Expected: each user has one active container and fixture settings reach the provider unchanged.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime/Application/ChallengeRuntimeService.cs \
    src/GZCTF/Features/ChallengeRuntime/Infrastructure/LegacyContainerRuntimeAdapter.cs \
    src/GZCTF.Integration.Test/Tests/Runtime
  git commit -m "feat: add learner container lifecycle"
  ```

## Task 5: Implement submission and global completion

- [ ] Add failing tests for rejected and accepted submissions, duplicate accepted submissions, one challenge referenced from two paths, and two concurrent correct submissions.
- [ ] Implement `ChallengeSubmissionService.cs` using the existing flag checker semantics for static flags, templates, dynamic attachment flags, and dynamic container flags.
- [ ] In one transaction, insert the submission, attempt to insert `ChallengeProgress`, and increment that user's daily solve stat only if progress was newly created.
- [ ] Determine solve mode from help usage at the first accepted submission: WP takes precedence over hint, and absence of both is independent.
- [ ] Treat the progress unique constraint as an expected race result. A losing concurrent request returns accepted with `firstSolve=false` and does not increment daily stats.
- [ ] Query path completion by joining module challenge references to global progress; do not write path specific challenge completion rows.
- [ ] Run the submission and concurrency tests. Expected: one global progress row and one daily increment for any number of duplicate or concurrent correct submissions.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime/Application/ChallengeSubmissionService.cs \
    src/GZCTF.Integration.Test/Tests/Runtime
  git commit -m "feat: record global challenge completion"
  ```

## Task 6: Implement hints and official WP

- [ ] Add failing `ChallengeHelpTests.cs` cases proving hints are ordered, individual hint views are recorded once per open event, WP first view time is stable, and neither action completes a challenge.
- [ ] Implement `ChallengeHelpService.cs`. Fetch localized content using requested locale, then English fallback. Record usage before returning protected content.
- [ ] Return hints one at a time so each reveal has a distinct audit event. Return the full WP from a dedicated action.
- [ ] Ensure public preview DTOs expose only counts indicating help availability, never hint or WP content.
- [ ] Run `ChallengeHelpTests`; expect help state and solve attribution to pass.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime/Application/ChallengeHelpService.cs \
    src/GZCTF.Integration.Test/Tests/Runtime/ChallengeHelpTests.cs
  git commit -m "feat: add challenge hints and writeups"
  ```

## Task 7: Expose the learner runtime API

- [ ] Add authenticated actions in `ChallengesController.cs`, `ChallengeInstancesController.cs`, and `ChallengeSubmissionsController.cs` using the service methods above.
- [ ] Add API authorization tests proving a learner can access only their own instance, submissions, help usage, and attachment assignment; an administrator can edit challenge content through a separate administrator controller; anonymous access is rejected.
- [ ] Add rate limits for submission and instance mutation using existing rate limit infrastructure.
- [ ] Register runtime services in `ServicesExtension.cs` and regenerate `ClientApp/src/Api.ts` from OpenAPI.
- [ ] Run the complete runtime integration test folder.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/ChallengeRuntime src/GZCTF/Extensions/Startup/ServicesExtension.cs \
    src/GZCTF/ClientApp/src/Api.ts src/GZCTF.Integration.Test/Tests/Runtime
  git commit -m "feat: expose learner challenge APIs"
  ```

## Wave 2 verification

- [ ] Run all four fixtures through HTTP endpoints with a real PostgreSQL container and test storage.
- [ ] Run container contract tests for both Docker and Kubernetes adapters without changing provider configuration names.
- [ ] Run each concurrency test at least 20 times to detect allocation and unique completion races.
- [ ] Inspect application logs and HTTP payloads for fixture flags; expected: flags appear only in the container injection boundary and internal validator, never in list responses or structured logs.
- [ ] Confirm route progress for a challenge referenced by two routes changes in both projections after one accepted submission.
