# Learning Platform Task Dispatch Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the approved six wave roadmap into ordered, independently reviewable work packets with explicit dependencies, file ownership, deliverables, tests, and handoff rules.

**Architecture:** Work flows through Foundation, Challenge Runtime, Migration, Learning Experience, Dashboard, and Legacy Decommission. Each packet has one primary responsibility and a green test boundary. Shared schema and generated API files have single writer ownership to prevent parallel edits from creating conflicting migrations or client output.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, SignalR, React 19, TypeScript, Vite, Mantine, SWR, ECharts, xUnit, Testcontainers, Playwright, Docker.

---

## 1. Source documents

- Product and architecture decisions: `docs/superpowers/specs/2026-09-19-learning-platform-refactor-design.md`
- Skill tree and shared category revision: `docs/superpowers/specs/2026-09-21-skill-tree-shared-categories-design.md`
- Release order and global gates: `docs/superpowers/plans/2026-09-19-learning-platform-roadmap.md`
- Wave plans:
  - `docs/superpowers/plans/2026-09-19-learning-foundation-plan.md`
  - `docs/superpowers/plans/2026-09-19-challenge-runtime-plan.md`
  - `docs/superpowers/plans/2026-09-19-legacy-migration-plan.md`
  - `docs/superpowers/plans/2026-09-19-learning-experience-plan.md`
  - `docs/superpowers/plans/2026-09-19-learning-dashboard-plan.md`
  - `docs/superpowers/plans/2026-09-19-legacy-decommission-plan.md`

If this dispatch plan and a wave plan differ, the approved design controls product behavior, the roadmap controls wave order, and the more detailed task document controls implementation mechanics.

## 2. Work roles

| Role | Primary responsibility | Owned paths while assigned |
|---|---|---|
| `FOUNDATION` | Shared API contract, canonical entities, EF mapping, route publication, enrollment, records | `Features/Shared`, `Features/ChallengeLibrary`, `Features/LearningPaths`, `Features/LearningProgress`, `Models/AppDbContext.cs`, active EF migration |
| `RUNTIME` | Four challenge modes, instances, submissions, help, global completion | `Features/ChallengeRuntime`, runtime integration tests |
| `MIGRATION` | Canonical import, database backfill, ZIP import, parity reports | `Features/Imports`, legacy fixtures, import tests |
| `WEB` | Learner and administrator React experience | `ClientApp/src/pages/learn`, `pages/challenges`, `pages/account/Learning.tsx`, `pages/admin/library`, `pages/admin/learning-paths`, `pages/admin/imports`, learning components and hooks |
| `DASHBOARD` | Daily solve model, cohort administration, tokens, snapshot, hub, chart | `Features/Dashboard`, dashboard pages, components, hooks, and tests |
| `RETIREMENT` | Remove old API, repositories, hubs, pages, and registrations | Legacy controllers, repositories, services, hubs, and game/team pages |
| `QA-RELEASE` | Golden fixtures, cross feature acceptance, container and migration gates | Cross wave test harness, release evidence, deployment contract tests |

One worker may perform several roles serially. A role is an ownership boundary, not a requirement to use a separate person or process.

## 3. Dispatch rules

- [ ] A task may be claimed only when every dependency is merged into the working branch and its tests are green.
- [ ] One worker owns `AppDbContext.cs`, `AppDbContextModelSnapshot.cs`, and the active migration pair at a time.
- [ ] One worker regenerates `ClientApp/src/Api.ts` at a time. Other frontend work consumes the last committed generated client.
- [ ] Do not edit files owned by another active packet. Send a small prerequisite change back to that packet's owner.
- [ ] A packet ends with its named focused tests, static checks for touched languages, `git diff --check`, and one focused commit.
- [ ] A handoff includes changed files, commands executed, outcomes, schema or API decisions, risks, and the next unblocked packet.
- [ ] No packet drops or rewrites legacy competition tables during this release.
- [ ] Any accepted change that alters an approved behavior must first update the design and every downstream acceptance test affected by it.

## 4. Ordered task queue

### Wave 1 — Foundation

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S01` | FOUNDATION | Approved docs | Establish .NET 10 unit baseline, add the stable `ApiError` contract, and remove database credential logging | Unit suite green; logs contain host/database only and never username, password, raw connection string, or raw exception message | Ready |
| `S02` | FOUNDATION | S01 | Add canonical challenge, localization, flag, hint, WP, lesson, route revision, module, item, enrollment, and progress entities | EF model compiles and invariant tests fail before migration implementation | Blocked by S01 |
| `S03` | FOUNDATION | S02 | Add EF constraints, cohort/dashboard/import bookkeeping, and the additive foundation migration | Fresh and legacy PostgreSQL schemas migrate without dropping an old table | Blocked by S02 |
| `S04` | FOUNDATION | S03 | Implement administrator challenge and lesson libraries plus manual duplicate merge | Secret fields stay protected; merge redirects references and reconciles progress without double counting | Blocked by S03 |
| `S05` | FOUNDATION | S04 | Implement route draft copy, validation, preview, row version conflict handling, and atomic publication | Published graph is immutable and pointer switch is transactional | Blocked by S04 |
| `S06` | FOUNDATION | S05 | Implement multiple enrollments, one current route, protected lesson reads, and idempotent lesson completion | Enrollment and lesson access tests pass | Blocked by S05 |
| `S07` | FOUNDATION | S06 | Implement personal learning record projection | Aggregates appear only in the account response; reused challenges count globally once and in each referenced route | Blocked by S06 |

Wave 1 gate: all Foundation integration tests pass against PostgreSQL, an anonymous preview contains no protected body or progress, and the old schema remains intact.

### Wave 2 — Challenge runtime

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S08` | QA-RELEASE | S07 | Commit deterministic fixtures and failing behavior tests for all four legacy challenge modes | Static attachment, dynamic attachment, static container, and dynamic container expectations are explicit | Blocked by S07 |
| `S09` | RUNTIME | S08 | Add learner instance, submission, help usage, and runtime migration; connect the Foundation global progress entity | Unique active instance and existing unique global completion constraints pass | Blocked by S08 |
| `S10` | RUNTIME | S09 | Implement static download and atomic dynamic attachment allocation | Parallel users receive different resources; repeated open is stable | Blocked by S09 |
| `S11` | RUNTIME | S10 | Implement canonical container adapter and idempotent start, extend, stop, status | Image, port, CPU, memory, storage, network, and dynamic flag settings match fixtures | Blocked by S10 |
| `S12` | RUNTIME | S11 | Implement flag validation, submissions, first solve transaction, and global completion | Concurrent correct submissions create one progress row and one daily increment | Blocked by S11 |
| `S13` | RUNTIME | S12 | Implement ordered hints, full WP, usage tracking, and solve attribution | Help never completes a challenge; first solve mode is independent, after hint, or after WP | Blocked by S12 |
| `S14` | RUNTIME | S13 | Expose authenticated challenge, instance, submission, hint, WP, and own history APIs | Authorization, ownership, rate limit, and four mode HTTP tests pass | Blocked by S13 |

Wave 2 gate: the four golden challenges work through HTTP, repeated concurrency runs remain stable, and flags appear in neither list responses nor structured logs.

### Wave 3 — Legacy migration

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S15` | QA-RELEASE | S14 | Create golden legacy database, ZIP, and expected parity fixtures | Fixtures contain all four modes, duplicate titles, every role, competition history, and known file hashes | Blocked by S14 |
| `S16` | MIGRATION | S15 | Define source neutral canonical import records and validators | Invalid type mixtures, unsafe paths, missing files, and duplicate source IDs are rejected | Blocked by S15 |
| `S17` | MIGRATION | S16 | Implement transactional, resumable canonical importer and source mappings | Same title stays separate; same source is idempotent; game becomes draft route grouped by category | Blocked by S16 |
| `S18` | MIGRATION | S17 | Implement existing PostgreSQL startup backfill and migration readiness check | Second startup creates nothing; accounts and admin rights remain; learning progress starts empty | Blocked by S17 |
| `S19` | MIGRATION | S18 | Implement secure legacy ZIP staging, import, retry, and compensation | Repeated upload returns the same batch; unsafe or failed import leaves no rows or staged blobs | Blocked by S18 |
| `S20` | MIGRATION | S19 | Implement field and file parity reports and completion enforcement | Type, content, hints, flags, attachment hashes, and container settings match source exactly | Blocked by S19 |

Wave 3 gate: database and ZIP inputs each pass two consecutive idempotency runs and their migrated challenges pass the Wave 2 contracts.

### Wave 4 — Learning and administration experience

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S21` | WEB | S20 | Add Playwright harness and anonymous, learner, administrator fixtures | A failing route discovery test proves the harness observes the real application | Blocked by S20 |
| `S22` | WEB | S21 | Build route discovery cards and route outline | Cards show summary counts; route pages show item checkmarks but no aggregate progress | Blocked by S21 |
| `S23` | WEB | S22 | Build enrollment controls, Markdown lesson workspace, and unrestricted previous/next navigation | Multiple routes and current route work; no item is hard locked | Blocked by S22 |
| `S24` | WEB | S23 | Build shared challenge workspace and standalone challenge page | Four modes, instance controls, submit, hints, and WP work in both contexts | Blocked by S23 |
| `S25` | WEB | S24 | Build account learning records | Route percentage, completed modules, completed lessons, solved count, solve mode, and recent activity appear only here | Blocked by S24 |
| `S26` | WEB | S25 | Build challenge, lesson, and route administrator screens | Administrator can author bilingual content, compose modules, preview, publish, and resolve row version conflict | Blocked by S25 |
| `S27` | WEB | S26 | Build import review, update logout invalidation, and switch primary navigation to Learning | ZIP report and draft route are reviewable; new UI uses Chinese and English fallback | Blocked by S26 |

Wave 4 gate: Chinese and English browser journeys pass for every role, protected content is absent from anonymous network responses, and aggregate progress placement is proven by negative assertions.

### Wave 5 — Dashboard

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S28` | DASHBOARD | S27 | Implement first solve daily projection and rebuild | Repeated or concurrent correct submissions count once; rebuild matches incremental data | Blocked by S27 |
| `S29` | DASHBOARD | S28 | Implement standard cohort CRUD, member search, and transactional bulk assignment | Each member has zero or one cohort and inactive cohorts are handled consistently | Blocked by S28 |
| `S30` | DASHBOARD | S29 | Implement bounded all member cumulative snapshot, search, cohort filter, ranking, and cache | Every selected member appears; each series has at most 120 points; ties are deterministic | Blocked by S29 |
| `S31` | DASHBOARD | S30 | Implement raw once, hash stored dashboard tokens with expiration, rotation, revocation, and rate limit | Raw token is absent from database and logs; revoked token stops snapshot access | Blocked by S30 |
| `S32` | DASHBOARD | S31 | Implement token protected SignalR deltas and reconnect recovery | First solve sends one delta; duplicates are silent; reconnect begins from a fresh snapshot | Blocked by S31 |
| `S33` | DASHBOARD | S32 | Build cohort admin, dashboard admin, all member Canvas chart, search highlight, and Top list | Public read only link works without login and exposes username plus solved count only | Blocked by S32 |

Wave 5 gate: privacy payload inspection, duplicate solve, all member curve, 24级/25级 filter, username highlight, Top 10/20, live update, and revoked link tests pass.

### Wave 6 — Legacy decommission and release

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `S34` | QA-RELEASE | S33 | Add failing legacy surface removal and old table retention tests | Exact API, hub, page, table, and row count boundaries are frozen | Blocked by S33 |
| `S35` | RETIREMENT | S34 | Remove game, team, exercise, old import/export HTTP actions and regenerate API client | Old routes are unavailable; Learning routes retain expected authorization | Blocked by S34 |
| `S36` | RETIREMENT | S35 | Remove legacy repositories, score/blood/cache services, and registrations after dependency audit | No runtime caller uses old repositories; migration source remains read only | Blocked by S35 |
| `S37` | RETIREMENT | S36 | Remove monitor and competition SignalR events and keep dashboard hub | Legacy monitor connection fails; dashboard works with and without Redis | Blocked by S36 |
| `S38` | RETIREMENT | S37 | Remove game, team, scoreboard, judge, monitor pages, hooks, strings, and navigation | Generated routes and visible links contain no competition experience | Blocked by S37 |
| `S39` | RETIREMENT | S38 | Enforce retained legacy tables as read only migration evidence | Old tables and rows remain and runtime writes are rejected | Blocked by S38 |
| `S40` | QA-RELEASE | S39 | Execute backend, frontend, migration, browser, image, port, health, Redis, storage, Docker, and Kubernetes release gates | Container still starts `dotnet GZCTF.dll`, application uses 8080, health uses 3000, and `GZCTF_` configuration remains compatible | Blocked by S39 |

Wave 6 gate: every roadmap release gate passes and the evidence package is attached to the change review.

## 5. Safe concurrency opportunities

The queue above is the merge order. The following preparation can happen in parallel without changing that order:

- `S08` fixture authoring may start after `S03` if it edits only `src/GZCTF.Integration.Test/Fixtures/Challenges` and does not assume unfinished runtime APIs.
- `S15` fixture authoring may start after `S03` if it edits only `src/GZCTF.Integration.Test/Fixtures/Legacy`.
- `S21` Playwright harness setup may start after `S14` if it adds infrastructure and failing tests without building product pages.
- The cohort section of `S33` may be mocked after the `S29` API response is fixed, but production pages wait for the merged generated client.
- `S34` boundary test design may start after `S20`, while executable negative tests wait for every replacement endpoint.

Never parallelize two tasks that edit `AppDbContext.cs`, the model snapshot, the same EF migration, `ServicesExtension.cs`, `AppNavbar.tsx`, `useUser.tsx`, or generated `Api.ts`.

## 6. Handoff format

Every completed packet adds a short handoff entry to its change review using this shape:

```text
Task: S01
Commit: SHA emitted by the completed git commit
Changed: exact files changed
Verified: exact commands and pass/fail counts
Decisions: implementation choices that downstream packets depend on
Risks: concrete unresolved risk, or the word “none”
Next: first newly unblocked task ID
```

The commit SHA is recorded after the commit is created; it is not preassigned in this document.

## 7. Definition of ready

A packet is ready when:

- [ ] Its dependencies are committed and their focused tests pass.
- [ ] The expected input API and database shape are committed.
- [ ] Its owned files are not being edited by another active packet.
- [ ] Test fixtures and external services named by the packet are available.
- [ ] The packet has no unanswered product decision.

## 8. Definition of done

A packet is done when:

- [ ] New behavior is introduced through a failing test first.
- [ ] The focused test passes and relevant existing tests still pass.
- [ ] No secret, flag, token hash, or private learner field appears in logs or unintended DTOs.
- [ ] Database changes are additive, indexed, constrained, and safe against the legacy fixture.
- [ ] New list queries use DTO projection and `AsNoTracking`.
- [ ] `git diff --check` passes.
- [ ] A focused commit and handoff entry exist.

## 9. First dispatch

Assign `S01` to the `FOUNDATION` owner using:

`docs/superpowers/plans/2026-09-19-step-01-baseline-and-security-development.md`

`S02` remains blocked until the S01 unit suite, secret scan, and commit are complete.
