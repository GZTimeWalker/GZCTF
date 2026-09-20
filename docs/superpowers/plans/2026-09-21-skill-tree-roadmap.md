# Skill Tree Refactor Implementation Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the implemented LearningPath and LearningModule structure with skill trees and globally shared categories without losing current learning data, challenge behavior, or deployment compatibility.

**Architecture:** Introduce a new `SkillTrees` domain beside the current Learning tables, backfill it idempotently, then move APIs and user interfaces in controlled waves. Skill tree revisions own only ordered category references; categories own globally ordered lesson and challenge references, so category content changes propagate immediately across published trees.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, React 19, TypeScript, Vite, Mantine, SWR, xUnit, Testcontainers, Playwright, Docker.

---

## Source of truth

- Approved design: `docs/superpowers/specs/2026-09-21-skill-tree-shared-categories-design.md`
- Task dispatch: `docs/superpowers/plans/2026-09-21-skill-tree-task-dispatch.md`
- First implementation packet: `docs/superpowers/plans/2026-09-21-skill-tree-foundation-plan.md`
- The 2026-09-21 design supersedes route/module behavior in the 2026-09-19 design.
- Existing challenge runtime, migration parity, dashboard, account, storage, container, and deployment contracts remain in scope as regression boundaries.

## Execution order

### Wave ST1 — Skill tree foundation and data migration

- [ ] Add `SkillTree`, `SkillTreeRevision`, `SkillCategory`, `SkillTreeCategoryRef`, `CategoryContent`, `SkillTreeEnrollment`, and `LearningPathRedirect` entities.
- [ ] Add the fixed icon catalog and API validation.
- [ ] Add EF constraints, indexes, soft-delete fields, and publication state for lessons.
- [ ] Add an additive migration that keeps current Learning tables intact.
- [ ] Backfill current paths, modules, items, enrollments, current selection, and old slugs idempotently.
- [ ] Prove fresh database, upgraded database, repeated migration, row-count parity, and no destructive legacy operation.

Gate: the complete solution builds; schema and backfill integration tests pass twice against PostgreSQL; current APIs still run unchanged.

### Wave ST2 — Skill tree and category application APIs

- [ ] Implement skill tree list, detail, draft, preview, empty publication, republishing, enrollment, and personal record APIs.
- [ ] Implement category CRUD, tree association, global ordering, delete impact, soft delete, and merge APIs.
- [ ] Extend challenge and lesson administration with draft/publication state and multi-category assignment.
- [ ] Implement publish-time inline category creation for an empty tree.
- [ ] Implement typed-name skill tree deletion with impact preview and current-tree clearing.
- [ ] Add stable conflict and validation error codes.
- [ ] Regenerate the TypeScript client from OpenAPI.

Gate: backend API and transaction tests cover every publish, delete, merge, conflict, and shared-content rule; old Learning APIs remain available until Wave ST4.

### Wave ST3 — Administrator experience

- [ ] Replace the route list with `/admin/skill-trees` and a name, summary, icon creation form.
- [ ] Replace Modules JSON with category cards, an existing-category picker, inline category creation, removal, drag sorting, preview, and publish.
- [ ] Add `/admin/skill-categories` for global editing, ownership inspection, content ordering, merge, and delete.
- [ ] Add category selection and inline creation to challenge and lesson publish dialogs.
- [ ] Add impact summaries and typed-name destructive confirmations.
- [ ] Maintain Simplified Chinese and English strings; other languages fall back to English.

Gate: administrator Playwright flows pass without entering a UUID or JSON document; browser console and failed-request logs are clean.

### Wave ST4 — Learner cutover and release

- [ ] Add `/skill-trees`, `/skill-trees/{id}`, content workspaces, empty state, enrollment, and current-tree controls.
- [ ] Redirect `/learn` and mapped `/learn/{slug}` URLs to the new pages.
- [ ] Rename navigation, account records, API DTOs, and visible copy from route/module to skill tree/category.
- [ ] Switch progress aggregation and cache invalidation to shared categories.
- [ ] Retire old LearningPath APIs and frontend generated types after redirect and parity tests pass.
- [ ] Run old-database, ZIP import, four challenge modes, dashboard, browser, image, port, health, Redis, storage, Docker, and Kubernetes release gates.

Gate: all unit and integration tests pass; migrated counts match; anonymous, learner, administrator, and dashboard journeys pass; container startup remains unchanged.

## Cross-wave rules

- [ ] Use test-first changes for every invariant, migration, publication, deletion, merge, redirect, and progress calculation.
- [ ] Keep the new schema additive until Wave ST4 parity gates pass.
- [ ] Never infer category identity from a display name.
- [ ] Preserve `ChallengeProgress(UserId, ChallengeId)` and `LessonProgress(UserId, LessonId)` as global completion truth.
- [ ] Keep skill tree structure revisions immutable after publication; category content remains live shared data by design.
- [ ] Use `AsNoTracking` and DTO projection for lists; batch-load tree details to avoid N+1 queries.
- [ ] Never return Flag values, token hashes, unpublished protected content, or private learner fields in list DTOs.
- [ ] Keep commits focused and green; regenerate `ClientApp/src/Api.ts` from a running OpenAPI document, not by hand.
- [ ] Do not change Dockerfile entrypoint, ports, configuration prefix, storage, Redis, Docker, or Kubernetes contracts.

## Release commands

Backend build and unit tests:

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet test GZCTF.Test/GZCTF.Test.csproj -c Debug
```

Local PostgreSQL integration tests:

```bash
docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  -e GZCTF_INTEGRATION_TEST_MODE=local \
  -e TESTCONTAINERS_RYUK_DISABLED=true \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Debug
```

Frontend checks:

```bash
cd src/GZCTF/ClientApp
pnpm test:unit
pnpm check
pnpm build
pnpm test:e2e
```

Final repository checks:

```bash
git diff --check
rg -n '/api/learning-paths|admin/learning-paths|pages/learn' src/GZCTF src/GZCTF/ClientApp/src
```

Expected final search result: only explicit redirect compatibility and migration mapping references remain.
