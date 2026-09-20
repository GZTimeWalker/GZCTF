# Skill Tree Refactor Task Dispatch Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the approved skill tree design into ordered, independently reviewable work packets with explicit ownership, dependencies, tests, and handoff criteria.

**Architecture:** Work proceeds through additive schema and backfill, backend application APIs, administrator UI, and learner cutover. Schema, migrations, generated API output, and shared translations each have a single writer; other packets can run in parallel only after their input contract is committed.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, React 19, TypeScript, Vite, Mantine, SWR, xUnit, Testcontainers, Playwright, Docker.

---

## 1. Source documents

- Approved design: `docs/superpowers/specs/2026-09-21-skill-tree-shared-categories-design.md`
- Release roadmap: `docs/superpowers/plans/2026-09-21-skill-tree-roadmap.md`
- First detailed packet: `docs/superpowers/plans/2026-09-21-skill-tree-foundation-plan.md`

If the documents differ, the approved design controls behavior, the roadmap controls wave order, and the detailed packet controls implementation mechanics.

## 2. Work roles and ownership

| Role | Responsibility | Owned paths while assigned |
|---|---|---|
| `TREE-FOUNDATION` | Entities, EF mapping, indexes, migrations, icon catalog | `Features/SkillTrees/Domain`, `Features/SkillTrees/Infrastructure`, `Models/AppDbContext.cs`, active migration |
| `TREE-MIGRATION` | Current Learning backfill, old slug mapping, migration parity | `Features/SkillTrees/Migration`, skill tree migration tests |
| `TREE-API` | Tree revisions, publication, enrollment, records, deletion | `Features/SkillTrees/Application`, `Features/SkillTrees/Api` |
| `CATEGORY-API` | Category CRUD, ordering, merge, delete impact | `Features/SkillCategories` or category files under `Features/SkillTrees` |
| `CONTENT-API` | Challenge and lesson publication and category assignment | `Features/ChallengeLibrary`, affected runtime detail DTOs |
| `ADMIN-WEB` | Skill tree, category, lesson, challenge administration | `ClientApp/src/pages/admin/skill-*`, learning admin components/hooks |
| `LEARNER-WEB` | Learner routes, navigation, records, redirects | `ClientApp/src/pages/skill-trees`, learning components/hooks, navigation |
| `API-CODEGEN` | OpenAPI generation and generated client | `ClientApp/src/Api.ts` |
| `QA-RELEASE` | Cross-feature, migration, browser, deployment gates | integration fixtures, E2E tests, release evidence |

One worker may perform roles serially. A role is an edit boundary, not a requirement for a separate person.

## 3. Dispatch rules

- [ ] A task starts only after every dependency is committed and its named tests are green.
- [ ] Only `TREE-FOUNDATION` edits `AppDbContext.cs`, the model snapshot, and the active migration pair.
- [ ] Only `API-CODEGEN` regenerates `ClientApp/src/Api.ts` after the backend contract is green.
- [ ] Do not remove current Learning tables or APIs before ST4 parity and redirect gates pass.
- [ ] Every task starts with a failing behavior or schema test and ends with a focused commit.
- [ ] Every handoff lists changed files, commands, output, schema/API decisions, risks, and the next unblocked task.
- [ ] Any behavior change first updates the approved design and affected acceptance tests.

## 4. Ordered task queue

### Wave ST1 — Foundation and migration

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `ST01` | TREE-FOUNDATION | Approved docs | Freeze schema, icon, publication, soft-delete, and empty-tree invariants with failing PostgreSQL tests | Tests fail for missing SkillTree tables and Lesson publication state | Ready |
| `ST02` | TREE-FOUNDATION | ST01 | Add skill tree/category domain entities and fixed icon catalog | Domain compiles; icon catalog rejects arbitrary values | Blocked by ST01 |
| `ST03` | TREE-FOUNDATION | ST02 | Add EF mappings, DbSets, uniqueness, check constraints, indexes, and additive migration | Fresh PostgreSQL migrates without dropping Learning or legacy competition tables | Blocked by ST02 |
| `ST04` | TREE-MIGRATION | ST03 | Backfill current paths, modules, items, enrollments, current selection, and slug redirect mappings | Counts and order match current Learning data | Blocked by ST03 |
| `ST05` | TREE-MIGRATION | ST04 | Make backfill idempotent and add fresh, upgraded, and repeated-run parity tests | Running startup migration twice changes no count or association | Blocked by ST04 |

Wave ST1 gate: execute the detailed foundation plan; full solution build and focused schema/migration tests pass; current product behavior remains available.

### Wave ST2 — Application and API

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `ST06` | TREE-API | ST05 | Implement public skill tree summary/detail queries with empty published trees | Anonymous responses expose no protected body or progress | Blocked by ST05 |
| `ST07` | TREE-API | ST06 | Implement admin create, draft category refs, ordering, preview, row-version conflicts, and empty publication | Published category structure switches atomically | Blocked by ST06 |
| `ST08` | CATEGORY-API | ST07 | Implement category create/edit, orphan categories, tree association, and global content ordering | Shared category appears in every referencing tree with one content order | Blocked by ST07 |
| `ST09` | CATEGORY-API | ST08 | Implement category impact preview, soft delete, and merge transaction | References dedupe; published audit refs remain; content/progress survive | Blocked by ST08 |
| `ST10` | CONTENT-API | ST08 | Add lesson publication state and challenge/lesson multi-category draft commands | Uncategorized drafts save; uncategorized publish fails with stable code | Blocked by ST08 |
| `ST11` | CONTENT-API | ST10 | Implement publish dialog command, tree-filter validation, and inline category creation | Publishing into an empty tree creates and attaches a category atomically | Blocked by ST10 |
| `ST12` | TREE-API | ST09, ST11 | Implement enrollment, current tree, live progress aggregation, delete impact, typed-name soft delete, and current-pointer clearing | Deleted tree hides; historical record and global progress remain | Blocked by ST09, ST11 |
| `ST13` | API-CODEGEN | ST12 | Expose OpenAPI tags and regenerate TypeScript client | Generated client contains SkillTree APIs and no handwritten additions | Blocked by ST12 |

Wave ST2 gate: all SkillTree, category, publication, enrollment, merge, delete, and concurrency integration tests pass against PostgreSQL.

### Wave ST3 — Administrator web experience

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `ST14` | ADMIN-WEB | ST13 | Build skill tree list and name/summary/icon creation form | Admin creates a tree without JSON, UUID, or locale fields | Blocked by ST13 |
| `ST15` | ADMIN-WEB | ST14 | Build visual tree editor with existing-category picker, inline create, removal, drag order, preview, and publish | Empty and populated tree publication both work | Blocked by ST14 |
| `ST16` | ADMIN-WEB | ST13 | Build global category list/editor with tree membership and content sorting | One reorder is visible in all referencing trees | Blocked by ST13 |
| `ST17` | ADMIN-WEB | ST16 | Build category merge and impact-aware delete flows | Duplicate categories merge; typed confirmations protect referenced data | Blocked by ST16 |
| `ST18` | ADMIN-WEB | ST13, ST16 | Replace challenge editor category fields and add publish modal | Admin selects tree, multi-selects categories, or creates one inline | Blocked by ST13, ST16 |
| `ST19` | ADMIN-WEB | ST18 | Add lesson draft/publication editor with the same category flow | Lesson and challenge behavior remain consistent | Blocked by ST18 |
| `ST20` | ADMIN-WEB | ST15, ST17 | Add skill tree deletion impact preview and typed-name confirmation | Soft deletion completes and list refreshes without data loss | Blocked by ST15, ST17 |
| `ST21` | QA-RELEASE | ST14-ST20 | Add administrator Playwright coverage and i18n key checks | No raw translation key or JSON editor appears | Blocked by ST14-ST20 |

Wave ST3 gate: administrator browser suite passes in Chinese and English with clean console and network logs.

### Wave ST4 — Learner cutover and release

| ID | Owner | Depends on | Work packet | Required result | Status |
|---|---|---|---|---|---|
| `ST22` | LEARNER-WEB | ST13 | Build `/skill-trees` discovery cards and empty published state | Visitor sees icon, name, summary and counts without progress | Blocked by ST13 |
| `ST23` | LEARNER-WEB | ST22 | Build tree detail grouped by shared categories and content workspace links | Shared category content/order matches across trees | Blocked by ST22 |
| `ST24` | LEARNER-WEB | ST23 | Replace enrollment/current-tree controls and personal record terminology | Existing migrated enrollments and progress render correctly | Blocked by ST23 |
| `ST25` | LEARNER-WEB | ST24 | Rename navigation icon/copy and add `/learn` plus slug redirects | Old bookmarks reach the correct new tree | Blocked by ST24 |
| `ST26` | TREE-API | ST25 | Retire old LearningPath APIs, services, generated types, and unused JSON helpers | Search leaves only redirect and migration references | Blocked by ST25 |
| `ST27` | QA-RELEASE | ST26 | Run data parity, four challenge modes, shared progress, dashboard, browser, and performance suites | All suites pass; no N+1 query or unbounded response | Blocked by ST26 |
| `ST28` | QA-RELEASE | ST27 | Build and inspect production image; verify ports, health, Redis, storage, Docker, and Kubernetes | Deployment contract remains unchanged | Blocked by ST27 |

Wave ST4 gate: migration and behavior parity are recorded; release image is ready for deployment.

## 5. Parallel execution windows

- After ST13, ST14, ST16, ST18, and ST22 may run in parallel because they consume the same committed generated API without sharing page files.
- ST17 depends on ST16 because it extends the category editor.
- ST19 depends on ST18 so challenge and lesson category flows use one shared component.
- ST20 depends on the tree editor and category impact contracts.
- ST26 is serial and owns removals; no frontend worker edits old Learning files during that task.
- ST27 and ST28 remain serial release gates.

## 6. Handoff template

Every completed packet reports:

```text
Packet:
Commit:
Changed files:
Commands and results:
Schema/API decisions:
Known limitations:
Next unblocked packet:
```
