# Legacy Competition Decommission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove competition, team, scoring, blood, judge, monitor, and legacy exercise runtime surfaces after migration while retaining old tables and the unchanged deployment contract.

**Architecture:** Remove HTTP, SignalR, background, repository, and React entry points from the outside inward. Migration source readers continue to map legacy tables read only, so EF entities and tables remain available for audit. The canonical learning runtime becomes the only writer for challenge activity.

**Tech Stack:** ASP.NET Core 10, EF Core 10, React 19, Vite, Docker, xUnit, Testcontainers, Playwright.

---

## File map

| File or directory | Responsibility |
|---|---|
| `src/GZCTF.Integration.Test/Tests/Decommission/LegacySurfaceTests.cs` | Prove removed routes and retained learning routes. |
| `src/GZCTF.Integration.Test/Tests/Decommission/LegacyTableRetentionTests.cs` | Prove migrations do not drop or write old tables. |
| `src/GZCTF.Integration.Test/Tests/Deployment/ContainerContractTests.cs` | Prove entrypoint, ports, and configuration compatibility. |
| `src/GZCTF/Controllers/GameController.cs` | Remove legacy game API. |
| `src/GZCTF/Controllers/TeamController.cs` | Remove team API. |
| `src/GZCTF/Controllers/ExerciseController.cs` | Remove unfinished exercise API. |
| `src/GZCTF/Repositories/Game*.cs` and matching interfaces | Remove game runtime repositories. |
| `src/GZCTF/Repositories/TeamRepository.cs` and `ParticipationRepository.cs` | Remove team and participation runtime repositories. |
| `src/GZCTF/Repositories/Exercise*.cs` and matching interfaces | Remove unfinished exercise repositories. |
| `src/GZCTF/Hubs/MonitorHub.cs` and `src/GZCTF/Hubs/Clients/IMonitorClient.cs` | Remove competition monitor stream. |
| `src/GZCTF/Extensions/Startup/ServicesExtension.cs` | Remove legacy registrations and preserve learning services. |
| `src/GZCTF/Extensions/Startup/AppExtensions.cs` | Remove legacy hub maps and keep dashboard hub. |
| `src/GZCTF/ClientApp/src/pages/games/` | Remove learner competition pages. |
| `src/GZCTF/ClientApp/src/pages/admin/games/` | Remove competition administration pages. |
| `src/GZCTF/ClientApp/src/pages/Teams.tsx` | Remove team page. |
| `src/GZCTF/ClientApp/src/components/AppNavbar.tsx` | Remove all temporary competition navigation. |
| `src/GZCTF/ClientApp/src/Api.ts` | Regenerate without legacy API operations. |
| `.github/copilot-instructions.md` | Align framework and architecture documentation with .NET 10 and Learning. |
| `src/GZCTF/Dockerfile` | Remain behaviorally unchanged; covered by contract tests. |

## Task 1: Freeze the removal boundary with failing tests

- [ ] Add `LegacySurfaceTests.cs` with expected `404` cases for `/api/game`, `/api/team`, `/api/exercise`, legacy game export/import actions, and `/hub/monitor`.
- [ ] In the same test, assert success for route preview, authenticated lesson, challenge runtime, import report, cohort, and dashboard endpoints.
- [ ] Add Playwright assertions that no visible link or client route points to `/games`, `/teams`, `/admin/games`, scoreboards, game monitors, or judge views.
- [ ] Add `LegacyTableRetentionTests.cs` that records old table names and row counts before the final migration and verifies the same tables and rows afterward.
- [ ] Run the tests before removal. Expected: the legacy surface negative cases fail because routes still exist, while retention succeeds.
- [ ] Commit:

  ```bash
  git add src/GZCTF.Integration.Test/Tests/Decommission src/GZCTF/ClientApp/tests/e2e
  git commit -m "test: define competition decommission boundary"
  ```

## Task 2: Remove competition and team HTTP entry points

- [ ] Delete `GameController.cs`, `TeamController.cs`, and `ExerciseController.cs`.
- [ ] Split any still required generic administrator action out of `EditController.cs` into the appropriate Learning administrator controller, then delete its game specific actions.
- [ ] Keep legacy ZIP input only through `Features/Imports/Api/ImportsController.cs`; remove game export and old game import actions.
- [ ] Regenerate OpenAPI and `ClientApp/src/Api.ts`. Search the generated client for `/api/game`, `/api/team`, and `/api/exercise`; expected: zero matches.
- [ ] Run `LegacySurfaceTests`; expected: all removed API URLs return `404` or `405` and every Learning URL keeps its expected authorization result.
- [ ] Commit:

  ```bash
  git add -A src/GZCTF/Controllers src/GZCTF/Features src/GZCTF/ClientApp/src/Api.ts \
    src/GZCTF.Integration.Test/Tests/Decommission/LegacySurfaceTests.cs
  git commit -m "refactor: remove competition HTTP APIs"
  ```

## Task 3: Remove legacy runtime services and repositories

- [ ] Use `rg` to produce a dependency list for each legacy repository and service registration before deletion. Move any storage or container operation still used by Learning behind the Wave 2 adapters.
- [ ] Delete game, game challenge, game instance, game event, game notice, team, participation, division, cheat, legacy submission, exercise challenge, and exercise instance repository implementations and interfaces after their nonmigration references reach zero.
- [ ] Remove `GameExportService`, `GameImportService`, competition cache handlers, competition flag queue processing, and score or blood calculations after their callers reach zero.
- [ ] Preserve shared blob, post, user, API token, log, mail, container provider, and traffic proxy services required by the new runtime.
- [ ] Update `ServicesExtension.cs` registrations and replace the old queued `Submission` channel with the canonical submission path if it has no remaining consumer.
- [ ] Run backend build and all Learning integration tests. Expected: no missing registration and no use of old repositories outside `LegacyDatabaseSource` read projections.
- [ ] Commit:

  ```bash
  git add -A src/GZCTF/Repositories src/GZCTF/Services src/GZCTF/Extensions/Startup/ServicesExtension.cs \
    src/GZCTF/Features/Imports/Infrastructure/LegacyDatabaseSource.cs
  git commit -m "refactor: remove competition runtime services"
  ```

## Task 4: Remove competition SignalR surfaces

- [ ] Add hub route assertions to `LegacySurfaceTests.cs` for monitor removal and dashboard availability.
- [ ] Delete `MonitorHub.cs`, `IMonitorClient.cs`, and game specific events from `IUserClient` and `IAdminClient` after new Learning notifications use their own contracts.
- [ ] Remove `/hub/monitor` from `AppExtensions.cs`; map `/hub/dashboard` to `DashboardHub`.
- [ ] Simplify `SignalRSinkExtension.cs` to retain only operational administrator logging if still used. Remove score, blood, game event, and team push methods.
- [ ] Run hub integration tests with and without Redis backplane configuration.
- [ ] Commit:

  ```bash
  git add -A src/GZCTF/Hubs src/GZCTF/Extensions/SignalRSinkExtension.cs \
    src/GZCTF/Extensions/Startup/AppExtensions.cs src/GZCTF.Integration.Test/Tests/Decommission
  git commit -m "refactor: remove competition realtime surfaces"
  ```

## Task 5: Remove competition and team pages

- [ ] Delete `ClientApp/src/pages/games/`, `ClientApp/src/pages/admin/games/`, and `ClientApp/src/pages/Teams.tsx`.
- [ ] Remove game, team, scoreboard, judge, and monitor entries from `AppNavbar.tsx`; remove the temporary migration feature flag introduced in Wave 4.
- [ ] Delete components and hooks whose imports become zero after page removal. Keep shared Markdown, attachment, instance, and proxy components now owned by the Learning challenge workspace.
- [ ] Remove game keyed SWR invalidation and legacy locale strings that have no remaining references.
- [ ] Run:

  ```bash
  cd src/GZCTF/ClientApp
  pnpm check
  pnpm build
  pnpm test:e2e
  ```

  Expected: build and browser tests pass; generated routes contain no competition or team pages.

- [ ] Commit:

  ```bash
  git add -A src/GZCTF/ClientApp
  git commit -m "refactor: remove competition web experience"
  ```

## Task 6: Keep legacy data as read only migration evidence

- [ ] Audit `AppDbContext` and keep legacy entity mappings required by `LegacyDatabaseSource`. Do not expose DbSets through application services other than the migration source.
- [ ] Add a `SaveChanges` interceptor or architecture test that rejects Added, Modified, or Deleted states for retained competition entities after migration mode completes.
- [ ] Add a migration regression test that upgrades the golden old database to the final schema and verifies no `DropTable` or destructive legacy column operation appears in the generated migration script.
- [ ] Run `LegacyTableRetentionTests` twice against the same upgraded database.
- [ ] Commit:

  ```bash
  git add src/GZCTF/Models src/GZCTF/Features/Imports src/GZCTF/Migrations \
    src/GZCTF.Integration.Test/Tests/Decommission/LegacyTableRetentionTests.cs
  git commit -m "refactor: retain legacy competition data read only"
  ```

## Task 7: Prove the deployment contract is unchanged

- [ ] Add `ContainerContractTests.cs` that parses `src/GZCTF/Dockerfile` and launches the built image against test PostgreSQL. Assert:
  - process command ends in `dotnet GZCTF.dll`;
  - application HTTP listens on 8080;
  - health and metrics listen on 3000;
  - `GZCTF_` environment variables configure PostgreSQL, Redis, storage, and container providers;
  - the existing health URL becomes ready after schema and legacy migration.
- [ ] Build and inspect:

  ```bash
  dotnet publish src/GZCTF/GZCTF.csproj -c Release \
    -o src/GZCTF/publish/linux/amd64 -r linux-musl-x64 --no-self-contained
  docker build -t gzctf-learning:local src/GZCTF
  docker inspect gzctf-learning:local --format '{{json .Config.Entrypoint}} {{json .Config.Cmd}} {{json .Config.ExposedPorts}}'
  ```

- [ ] Start the image with the same compose or `docker run` arguments used by current GZCTF and exercise route preview, challenge runtime, imports, and health endpoints.
- [ ] Update `.github/copilot-instructions.md` from ASP.NET Core 9 to .NET 10 and replace game architecture guidance with the feature folder and migration boundaries.
- [ ] Run `ContainerContractTests` and commit:

  ```bash
  git add src/GZCTF.Integration.Test/Tests/Deployment/ContainerContractTests.cs \
    .github/copilot-instructions.md
  git commit -m "test: preserve container deployment contract"
  ```

## Wave 6 verification

- [ ] Run `rg -n '/api/game|/api/team|/api/exercise|/games|/teams|Scoreboard|MonitorHub' src/GZCTF src/GZCTF/ClientApp/src` and classify the only permitted matches as legacy migration entity names or historical source metadata.
- [ ] Run the complete backend, frontend, integration, and browser suites from the roadmap.
- [ ] Upgrade the golden database, run migration twice, and execute all four migrated challenges.
- [ ] Confirm account sign in and administrator privileges are unchanged and every learner has zero migrated progress before solving a new challenge.
- [ ] Confirm old competition tables and row counts remain available for audit and cannot be written by runtime endpoints.
- [ ] Inspect the final image and record entrypoint, port, environment, health, Redis, storage, Docker, and Kubernetes results in the release review.
