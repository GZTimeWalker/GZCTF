# Learning Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide an administrator managed read only dashboard with one cumulative unique challenge solve line per member, cohort filtering, username search, and Top 10 or Top 20 ranking.

**Architecture:** `ChallengeProgress` remains the source of truth. A transactional daily read model receives only first solves and can be rebuilt. Snapshot APIs return privacy limited, downsampled series; SignalR pushes small deltas. Dashboard links carry random bearer tokens whose hashes alone are stored and which can be expired, revoked, and rotated.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, SignalR, React 19, ECharts 6 Canvas renderer, Playwright, xUnit, Testcontainers.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Features/Dashboard/Application/DailySolveProjection.cs` | First solve projection and rebuild. |
| `src/GZCTF/Features/Dashboard/Application/DashboardSnapshotService.cs` | Filters, cumulative series, downsampling, ranking, and cache. |
| `src/GZCTF/Features/Dashboard/Application/DashboardTokenService.cs` | Generate, hash, validate, rotate, revoke, and expire access tokens. |
| `src/GZCTF/Features/Dashboard/Api/AdminCohortsController.cs` | Standard cohort CRUD and member assignment. |
| `src/GZCTF/Features/Dashboard/Api/AdminDashboardsController.cs` | Dashboard settings and token lifecycle. |
| `src/GZCTF/Features/Dashboard/Api/DashboardsController.cs` | Token protected snapshot endpoint. |
| `src/GZCTF/Features/Dashboard/Api/DashboardHub.cs` | Token protected solve and ranking deltas. |
| `src/GZCTF/Features/Dashboard/Infrastructure/DashboardCache.cs` | Short lived snapshot cache and precise invalidation. |
| `src/GZCTF.Integration.Test/Tests/Dashboard/DailySolveProjectionTests.cs` | Unique count and rebuild tests. |
| `src/GZCTF.Integration.Test/Tests/Dashboard/DashboardSnapshotTests.cs` | Cohort, series, ranking, privacy, and bounds. |
| `src/GZCTF.Integration.Test/Tests/Dashboard/DashboardTokenTests.cs` | Token lifecycle and hub authorization. |
| `src/GZCTF/ClientApp/src/pages/admin/cohorts/Index.tsx` | Cohort creation and bulk member assignment. |
| `src/GZCTF/ClientApp/src/pages/admin/dashboards/Index.tsx` | Dashboard configuration and link management. |
| `src/GZCTF/ClientApp/src/pages/dashboard/[id].tsx` | Public read only display. |
| `src/GZCTF/ClientApp/src/components/dashboard/SolveTrendChart.tsx` | All member cumulative lines and highlight. |
| `src/GZCTF/ClientApp/src/components/dashboard/SolveLeaderboard.tsx` | Deterministic Top ranking. |
| `src/GZCTF/ClientApp/src/hooks/useDashboard.ts` | Snapshot and SignalR delta state. |
| `src/GZCTF/ClientApp/tests/e2e/dashboard.spec.ts` | Dashboard administration and display acceptance. |

## Task 1: Project only first solves into daily statistics

- [ ] Add failing `DailySolveProjectionTests.cs` cases for one first solve, repeated correct submissions, concurrent correct submissions, different challenges on one day, cohort change, and full rebuild.
- [ ] Implement `DailySolveProjection.cs` so the first inserted `ChallengeProgress` increments exactly one `(UserId, Date)` row in the same transaction. Repeated accepted submissions perform no projection write.
- [ ] Add a rebuild command that truncates only the daily read model and groups `ChallengeProgress` by UTC date. Compare rebuilt rows to incremental rows in the test.
- [ ] Add a protected administrator endpoint to trigger rebuild and return inserted row count and maximum source timestamp.
- [ ] Run the projection test repeatedly under concurrency; expected: total daily increments equal distinct challenge progress rows.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Dashboard/Application/DailySolveProjection.cs \
    src/GZCTF.Integration.Test/Tests/Dashboard/DailySolveProjectionTests.cs
  git commit -m "feat: project learner daily solves"
  ```

## Task 2: Add cohort administration

- [ ] Add failing API tests to `DashboardSnapshotTests.cs` for unique active cohort names, one cohort per member, single assignment, bulk assignment, clearing assignment, and deactivated cohort filtering.
- [ ] Implement `AdminCohortsController.cs` with administrator only list, create, rename, activate, deactivate, member search, and batch assignment endpoints.
- [ ] Validate every member ID before applying a batch and update the batch in one transaction.
- [ ] Invalidate affected dashboard snapshots after cohort changes.
- [ ] Build `/admin/cohorts/Index.tsx` with a standard cohort list, username search, selection, and bulk assignment. Do not add real name or student number fields.
- [ ] Add browser coverage for creating “24级” and “25级” and assigning multiple users.
- [ ] Run API, browser, and TypeScript tests; commit:

  ```bash
  git add src/GZCTF/Features/Dashboard/Api/AdminCohortsController.cs \
    src/GZCTF/ClientApp/src/pages/admin/cohorts src/GZCTF.Integration.Test/Tests/Dashboard \
    src/GZCTF/ClientApp/tests/e2e/dashboard.spec.ts
  git commit -m "feat: manage learner cohorts"
  ```

## Task 3: Build bounded dashboard snapshots

- [ ] Add failing snapshot tests with at least three users, tied scores, inactive and empty cohorts, 200 daily points, and usernames containing non ASCII characters.
- [ ] Implement `DashboardSnapshotService.cs` with optional active cohort filter and case insensitive username search.
- [ ] Return one cumulative series for every selected member, including members with zero solves. Use daily points for short ranges and deterministic weekly sampling for long ranges so each member returns at most 120 points.
- [ ] Rank by unique solved challenge count descending, first time reaching that count ascending, then username ordinal ascending. Respect only configured Top 10 or Top 20.
- [ ] Return only dashboard ID, generated time, active cohort choices, username, cohort ID, cumulative time points, rank, and unique solved count.
- [ ] Add `DashboardCache.cs` with a short expiration and invalidation on first solve, cohort assignment, dashboard setting change, and projection rebuild.
- [ ] Implement `DashboardsController.cs` snapshot action and add a response size test for the approved maximum seeded member count.
- [ ] Run snapshot tests. Expected: all selected users appear, no series exceeds 120 points, tie order is stable, and private user fields do not serialize.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Features/Dashboard/Application/DashboardSnapshotService.cs \
    src/GZCTF/Features/Dashboard/Infrastructure/DashboardCache.cs \
    src/GZCTF/Features/Dashboard/Api/DashboardsController.cs \
    src/GZCTF.Integration.Test/Tests/Dashboard/DashboardSnapshotTests.cs
  git commit -m "feat: serve bounded learning dashboard snapshots"
  ```

## Task 4: Implement revocable read only links

- [ ] Add failing `DashboardTokenTests.cs` cases for creation, successful snapshot access, wrong token, expiration, revocation, rotation, rate limit, and confirmation that raw token is absent from the database and application logs.
- [ ] Implement `DashboardTokenService.cs` with 32 random bytes encoded using base64url. Hash tokens with SHA-256 plus a server side purpose prefix before storage.
- [ ] Return the raw token only from create and rotate responses. Store hash, creation, optional expiration, revocation, and last used timestamps.
- [ ] Implement administrator dashboard settings and token endpoints in `AdminDashboardsController.cs`. Restrict Top size to 10 or 20.
- [ ] Validate bearer query tokens for the snapshot and hub without granting access to any other API.
- [ ] Apply an independent dashboard rate limit partitioned by token hash and client address.
- [ ] Run token tests and commit:

  ```bash
  git add src/GZCTF/Features/Dashboard/Application/DashboardTokenService.cs \
    src/GZCTF/Features/Dashboard/Api/AdminDashboardsController.cs \
    src/GZCTF.Integration.Test/Tests/Dashboard/DashboardTokenTests.cs
  git commit -m "feat: add revocable dashboard links"
  ```

## Task 5: Push first solve deltas over SignalR

- [ ] Add failing hub tests for valid token connect, invalid or revoked token rejection, first solve delta, duplicate accepted submission silence, cohort change refresh, and reconnect snapshot recovery.
- [ ] Implement `DashboardHub.cs` with a token scoped group and no client mutation methods.
- [ ] After the first solve transaction commits, publish a delta containing username, cohort ID, UTC date, and new unique count. Publish a settings refresh marker after cohort or Top size changes.
- [ ] Keep PostgreSQL as truth. On reconnect, the client fetches a fresh snapshot before applying later deltas.
- [ ] Register the hub and ensure Redis backplane configuration continues to apply through existing SignalR registration.
- [ ] Run hub tests and commit:

  ```bash
  git add src/GZCTF/Features/Dashboard/Api/DashboardHub.cs src/GZCTF/Features/ChallengeRuntime \
    src/GZCTF/Extensions/Startup/ServicesExtension.cs src/GZCTF.Integration.Test/Tests/Dashboard
  git commit -m "feat: stream dashboard solve updates"
  ```

## Task 6: Build the dashboard administration and display pages

- [ ] Add failing Playwright cases for creating a dashboard, choosing Top 10 and Top 20, creating a link, opening it without login, filtering “24级”, searching a username, highlighting that line, receiving a live solve, and losing access after revocation.
- [ ] Build `/admin/dashboards/Index.tsx` with display settings, link create, copy, expiration, rotate, and revoke actions. Display a raw token only immediately after create or rotation.
- [ ] Build `useDashboard.ts` to fetch snapshot, establish SignalR, apply monotonic solve deltas, and refetch after reconnect or refresh markers.
- [ ] Build `SolveTrendChart.tsx` with ECharts Canvas. Render all selected member lines at low opacity and highlight the searched or hovered username.
- [ ] Build `SolveLeaderboard.tsx` with username and unique solved count only.
- [ ] Build `/dashboard/[id].tsx` as a read only full screen page with cohort selector, username search, chart, leaderboard, connection state, and last update time.
- [ ] Run dashboard browser tests, `pnpm check`, and `pnpm build`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/pages/admin/dashboards src/GZCTF/ClientApp/src/pages/dashboard \
    src/GZCTF/ClientApp/src/components/dashboard src/GZCTF/ClientApp/src/hooks/useDashboard.ts \
    src/GZCTF/ClientApp/tests/e2e/dashboard.spec.ts
  git commit -m "feat: add learning activity dashboard"
  ```

## Wave 5 verification

- [ ] Seed the approved maximum member count and measure snapshot query count, response size, and render time. Record the result and verify series remain bounded.
- [ ] Submit the same correct flag repeatedly and concurrently. Expected: one curve increment and one leaderboard count change.
- [ ] Filter each cohort and verify every assigned member appears, including zero solve members.
- [ ] Search an exact and partial username and verify only the matching line becomes prominent while all member lines remain visible.
- [ ] Revoke an open dashboard token. Expected: the next snapshot and SignalR reconnect are rejected and the browser displays an access expired state.
- [ ] Inspect the snapshot payload and browser DOM for email, real name, student number, IP, raw submissions, and token hash. Expected: none are present.
