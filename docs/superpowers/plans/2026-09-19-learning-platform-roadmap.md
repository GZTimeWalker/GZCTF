# Learning Platform Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the competition user experience with a route based learning platform while preserving accounts, administrator privileges, four challenge runtime modes, and both supported legacy import paths.

**Architecture:** Build six vertical execution waves on top of the existing ASP.NET Core monolith and React client. New Learning, Challenge, Import, and Dashboard features own their data and API boundaries; existing Identity, storage, container providers, telemetry, and deployment contracts remain shared infrastructure until the legacy competition runtime is removed in the final wave.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, SignalR, React 19, TypeScript, Vite, Mantine, SWR, ECharts, xUnit, Testcontainers, Playwright, Docker.

---

## Source of truth

- Approved design: `docs/superpowers/specs/2026-09-19-learning-platform-refactor-design.md`
- Task dispatch queue: `docs/superpowers/plans/2026-09-19-learning-platform-task-dispatch.md`
- First ready packet: `docs/superpowers/plans/2026-09-19-step-01-baseline-and-security-development.md`
- This roadmap fixes the execution order and release gates.
- Each linked plan is independently executable but must be completed in the order below because later waves consume APIs and tables created earlier.

## Execution order

- [ ] **Wave 1 — Foundation:** Execute `docs/superpowers/plans/2026-09-19-learning-foundation-plan.md`.
  - Establish the .NET 10 test baseline.
  - remove connection string logging.
  - Add canonical challenge, lesson, route revision, enrollment, progress, cohort, dashboard, and migration tables.
  - Add public route preview and administrator draft and publish APIs.
- [ ] **Wave 2 — Challenge runtime:** Execute `docs/superpowers/plans/2026-09-19-challenge-runtime-plan.md`.
  - Preserve static attachment, dynamic attachment, static container, and dynamic container behavior.
  - Add on demand user instances, submissions, global completion, hints, WP, and help attribution.
- [ ] **Wave 3 — Migration:** Execute `docs/superpowers/plans/2026-09-19-legacy-migration-plan.md`.
  - Backfill an existing PostgreSQL volume idempotently before readiness.
  - Import legacy game ZIP packages through the same canonical mapper.
  - Prove field, attachment hash, flag, and container configuration parity.
- [ ] **Wave 4 — Learning and administration experience:** Execute `docs/superpowers/plans/2026-09-19-learning-experience-plan.md`.
  - Replace competition navigation with learning discovery, workspace, account learning records, and administrator editors.
  - Preserve visitor and learner access boundaries and the approved progress display rules.
- [ ] **Wave 5 — Dashboard:** Execute `docs/superpowers/plans/2026-09-19-learning-dashboard-plan.md`.
  - Add cohorts, all member cumulative solve curves, username search, Top 10 or Top 20 ranking, read only revocable links, and SignalR deltas.
- [ ] **Wave 6 — Legacy decommission:** Execute `docs/superpowers/plans/2026-09-19-legacy-decommission-plan.md`.
  - Remove game, team, score, blood, judge, and competition monitor runtime surfaces.
  - Keep legacy tables and source mappings for audit in the first release.
  - Prove the existing Docker command, ports, configuration prefix, storage, Redis, and container provider contracts remain valid.

## Cross-wave rules

- [ ] Keep each commit green. Run the narrow test named by the task before its commit and the wave gate before moving to the next plan.
- [ ] New domain list queries use `AsNoTracking` and DTO projection. Do not add `AutoInclude` to new entities.
- [ ] Never return flags, token hashes, database credentials, or private learner fields from list and preview DTOs.
- [ ] Every write command is idempotent or protected by a uniqueness constraint plus a stable conflict response.
- [ ] Preserve `ChallengeProgress(UserId, ChallengeId)` as the only source of challenge completion truth.
- [ ] Treat `LearnerDailySolveStat` as a rebuildable read model, never as completion truth.
- [ ] Keep published route revisions immutable. Editing creates or changes the single draft revision and publishing atomically switches the route pointer.
- [ ] Do not drop legacy competition tables in this release.

## Release gates

- [ ] Run the full backend suite in .NET 10:

  ```bash
  docker run --rm \
    -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet test GZCTF.sln --configuration Release
  ```

  Expected: every unit test passes. Integration tests that require Docker are run by the dedicated command in each wave.

- [ ] Run frontend static checks and production build:

  ```bash
  cd src/GZCTF/ClientApp
  pnpm check
  pnpm build
  ```

  Expected: both commands exit `0` with no TypeScript or lint errors.

- [ ] Run the browser acceptance suite:

  ```bash
  cd src/GZCTF/ClientApp
  pnpm test:e2e
  ```

  Expected: visitor, learner, administrator, challenge, migration review, account learning, and dashboard journeys pass.

- [ ] Build the production image and assert its public contract:

  ```bash
  docker run --rm -v "$PWD:/workspace" -w /workspace \
    mcr.microsoft.com/dotnet/sdk:10.0-alpine \
    dotnet publish src/GZCTF/GZCTF.csproj -c Release \
      -o src/GZCTF/publish/linux/amd64 -r linux-musl-x64 --no-self-contained
  docker build -t gzctf-learning:local src/GZCTF
  docker inspect gzctf-learning:local --format '{{json .Config.Entrypoint}} {{json .Config.Cmd}} {{json .Config.ExposedPorts}}'
  ```

  Expected: the image starts `GZCTF.dll`; HTTP remains on `8080/tcp`; health and metrics remain on `3000/tcp`.

- [ ] Restore the approved legacy database fixture, start the image twice against the same volume, and compare migration reports.

  Expected: the second start creates no new challenges, routes, flags, attachments, or mappings; accounts and administrator privileges are unchanged; learning progress is empty.

- [ ] Import the approved ZIP fixture twice and compare reports.

  Expected: the second import returns the first completed batch; challenge fields, attachment hashes, flags, hints, and container settings match the source package.

## Completion evidence

The release is complete only when the following artifacts are attached to the change review:

- Backend and frontend command logs from the gates above.
- The in place database migration parity report.
- The ZIP import parity report.
- Four challenge mode acceptance results.
- A screenshot or browser trace proving aggregate progress appears only on `/account/learning`.
- A dashboard trace proving duplicate correct submissions do not add points and revoked links stop working.
- A production image inspection proving the startup contract is unchanged.
