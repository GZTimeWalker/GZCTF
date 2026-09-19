# Learning and Administration Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the learner and administrator web experience for route discovery, continuous study, challenge work, personal records, content editing, publication, and migration review.

**Architecture:** Keep file based routing and Mantine. Page components compose small domain components backed by generated API clients and scoped SWR hooks. Public preview, protected content, personal progress, and administrator editing use separate endpoints so the UI cannot accidentally expose restricted fields. Playwright covers the approved role and display rules.

**Tech Stack:** React 19, TypeScript 7, Vite 8, Mantine 9, SWR 2, i18next, marked/Shiki, Playwright, generated OpenAPI client.

---

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/ClientApp/package.json` | Browser test scripts and dependencies. |
| `src/GZCTF/ClientApp/playwright.config.ts` | Browser projects, base URL, traces, and test server. |
| `src/GZCTF/ClientApp/tests/e2e/learning.spec.ts` | Visitor and learner acceptance journey. |
| `src/GZCTF/ClientApp/tests/e2e/admin-learning.spec.ts` | Content, route, publish, and import administration journey. |
| `src/GZCTF/ClientApp/src/hooks/useLearning.ts` | Public path, enrollment, lesson, challenge, and progress SWR hooks. |
| `src/GZCTF/ClientApp/src/hooks/useAdminLearning.ts` | Administrator challenge, lesson, route, and import hooks. |
| `src/GZCTF/ClientApp/src/pages/learn/Index.tsx` | Route discovery and enrolled routes. |
| `src/GZCTF/ClientApp/src/pages/learn/[slug]/Index.tsx` | Route outline with item completion markers only. |
| `src/GZCTF/ClientApp/src/pages/learn/[slug]/[moduleId]/[itemId].tsx` | Continuous lesson or challenge workspace. |
| `src/GZCTF/ClientApp/src/pages/challenges/[id].tsx` | Standalone challenge workspace. |
| `src/GZCTF/ClientApp/src/pages/account/Learning.tsx` | Aggregate progress and recent learning. |
| `src/GZCTF/ClientApp/src/components/learning/RouteCard.tsx` | Public route summary and counts. |
| `src/GZCTF/ClientApp/src/components/learning/RouteOutline.tsx` | Modules, ordered items, and checkmarks. |
| `src/GZCTF/ClientApp/src/components/learning/LessonWorkspace.tsx` | Protected Markdown lesson and completion action. |
| `src/GZCTF/ClientApp/src/components/learning/ChallengeWorkspace.tsx` | Content, resources, instance, submit, hints, and WP. |
| `src/GZCTF/ClientApp/src/components/learning/LearningRecord.tsx` | Route and module completion metrics and activity. |
| `src/GZCTF/ClientApp/src/pages/admin/library/challenges/Index.tsx` | Canonical challenge library. |
| `src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx` | Challenge editor. |
| `src/GZCTF/ClientApp/src/pages/admin/library/lessons/Index.tsx` | Lesson library. |
| `src/GZCTF/ClientApp/src/pages/admin/library/lessons/[id].tsx` | Bilingual Markdown lesson editor. |
| `src/GZCTF/ClientApp/src/pages/admin/learning-paths/Index.tsx` | Path and publication status list. |
| `src/GZCTF/ClientApp/src/pages/admin/learning-paths/[id].tsx` | Draft composer, preview, and publish action. |
| `src/GZCTF/ClientApp/src/pages/admin/imports/Index.tsx` | ZIP upload and migration reports. |
| `src/GZCTF/ClientApp/src/components/AppNavbar.tsx` | Learning first navigation. |
| `src/GZCTF/ClientApp/src/locales/zh-CN/learning.json` | Simplified Chinese copy. |
| `src/GZCTF/ClientApp/src/locales/en-US/learning.json` | English copy and fallback. |

## Task 1: Add browser test infrastructure and role fixtures

- [ ] Add `@playwright/test` to development dependencies and scripts `test:e2e` and `test:e2e:ui` to `package.json`.
- [ ] Add `playwright.config.ts` with Chromium, a retained trace on failure, `http://127.0.0.1:8080` base URL, and the repository's Docker test environment as the web server command.
- [ ] Add `tests/e2e/support/auth.ts` that creates anonymous, learner, and administrator contexts through test only seeded accounts. Do not bypass production authorization in application code.
- [ ] Add the first failing case to `learning.spec.ts`: the `/learn` page is reachable anonymously and contains no aggregate progress percentage.
- [ ] Run:

  ```bash
  cd src/GZCTF/ClientApp
  pnpm install
  pnpm exec playwright install chromium
  pnpm test:e2e --grep "route discovery"
  ```

  Expected before implementation: the route is missing and the test fails for that reason.

- [ ] Commit the test harness:

  ```bash
  git add src/GZCTF/ClientApp/package.json src/GZCTF/ClientApp/pnpm-lock.yaml \
    src/GZCTF/ClientApp/playwright.config.ts src/GZCTF/ClientApp/tests
  git commit -m "test: add learning browser acceptance harness"
  ```

## Task 2: Build public discovery and route outline

- [ ] Extend `learning.spec.ts` with assertions that route cards show description, module count, total item count, challenge count, and expected time. Assert they do not show route percentage, completed modules, or completed lessons.
- [ ] Add `useLearning.ts` hooks for route list and preview, with locale included in the SWR key.
- [ ] Build `RouteCard.tsx`, `RouteOutline.tsx`, `/learn/Index.tsx`, and `/learn/[slug]/Index.tsx`.
- [ ] Show a checkmark beside an item only when an authenticated response contains that item's completion state. Never render route percentage or `completed / total` aggregate cards on these pages.
- [ ] For anonymous users, show route and module descriptions and item titles. Send lesson body and challenge body requests only after authentication.
- [ ] Add English fallback when a requested locale has no translation; configure every legacy UI locale other than `zh-CN` to fall back to `en-US` for new namespaces.
- [ ] Run the discovery browser cases and `pnpm check`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/hooks/useLearning.ts src/GZCTF/ClientApp/src/components/learning \
    src/GZCTF/ClientApp/src/pages/learn src/GZCTF/ClientApp/src/locales src/GZCTF/ClientApp/tests/e2e/learning.spec.ts
  git commit -m "feat: add learning route discovery"
  ```

## Task 3: Build enrollment, lesson, and continuous navigation

- [ ] Add failing browser cases for joining two routes, selecting a current route, opening any module without prerequisite enforcement, completing a lesson, and navigating to the previous and next content item.
- [ ] Add enrollment mutations and protected lesson hooks to `useLearning.ts`; invalidate only enrollment, outline completion, and account learning keys after changes.
- [ ] Build `LessonWorkspace.tsx` with localized Markdown, code highlighting, explicit “Mark complete”, and stable completed state.
- [ ] Build `/learn/[slug]/[moduleId]/[itemId].tsx` to select `LessonWorkspace` or `ChallengeWorkspace` by item kind and to render previous and next navigation across module boundaries.
- [ ] Make item ordering advisory. Do not disable links based on progress.
- [ ] Run focused browser cases and `pnpm check`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/hooks/useLearning.ts src/GZCTF/ClientApp/src/components/learning/LessonWorkspace.tsx \
    'src/GZCTF/ClientApp/src/pages/learn/[slug]/[moduleId]/[itemId].tsx' \
    src/GZCTF/ClientApp/tests/e2e/learning.spec.ts
  git commit -m "feat: add continuous lesson workspace"
  ```

## Task 4: Build the complete challenge workspace

- [ ] Add failing browser cases for each challenge mode, rejected and accepted flags, instance start/extend/stop, attachment download, ordered hint reveal, full WP view, and solve attribution display.
- [ ] Build `ChallengeWorkspace.tsx` as sections for statement, resources, instance, submission, hints, and official WP. Keep API errors mapped by stable error code.
- [ ] Reuse the same component in the continuous route workspace and `/challenges/[id].tsx`.
- [ ] Make hints and WP available at any time for authenticated users. Display a small recorded status after open; do not mark the challenge complete until an accepted flag.
- [ ] After the first solve, invalidate challenge progress, every route outline containing the challenge, account learning records, and dashboard data through named SWR key helpers.
- [ ] Run the challenge browser cases and `pnpm check`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/components/learning/ChallengeWorkspace.tsx \
    'src/GZCTF/ClientApp/src/pages/challenges/[id].tsx' src/GZCTF/ClientApp/src/hooks/useLearning.ts \
    src/GZCTF/ClientApp/tests/e2e/learning.spec.ts
  git commit -m "feat: add learner challenge workspace"
  ```

## Task 5: Put aggregate progress only in account learning records

- [ ] Add failing browser assertions that `/account/Learning` shows route percentage, completed modules, completed lessons, solved unique challenges, solve mode, and recent activity.
- [ ] Add negative assertions for `/learn`, `/learn/{slug}`, and the continuous workspace so aggregate route metrics cannot regress onto those pages.
- [ ] Build `LearningRecord.tsx` and `/account/Learning.tsx`. Calculate display values from server projections for the current published revision.
- [ ] Show one challenge solve in every referenced route while counting it once in the global solved challenge total.
- [ ] Add the account learning link to the existing account navigation.
- [ ] Run all learner browser cases and `pnpm check`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/components/learning/LearningRecord.tsx \
    src/GZCTF/ClientApp/src/pages/account/Learning.tsx src/GZCTF/ClientApp/src/components \
    src/GZCTF/ClientApp/tests/e2e/learning.spec.ts
  git commit -m "feat: add personal learning records"
  ```

## Task 6: Build administrator libraries and route composer

- [ ] Add failing `admin-learning.spec.ts` cases for creating bilingual lessons, creating each challenge type, adding ordered hints and WP, composing modules, reordering items, previewing a draft, publishing, and editing a new draft after publication.
- [ ] Add `useAdminLearning.ts` with separate keys for challenge library, lesson library, route list, route draft, draft preview, and import batches.
- [ ] Build challenge and lesson list and editor pages. Split challenge form sections into `ChallengeContentForm.tsx`, `ChallengeRuntimeForm.tsx`, and `ChallengeHelpForm.tsx` under `components/admin/learning`.
- [ ] Build path list and composer pages. Split composer sections into `PathMetadataForm.tsx`, `ModuleComposer.tsx`, `ContentPicker.tsx`, and `PublishReview.tsx`.
- [ ] Display row version conflicts as a reload and compare action; do not silently overwrite another administrator's draft.
- [ ] Require both Chinese or English fallback rules defined by the API and display publication validation next to the exact module or item.
- [ ] Run administrator browser cases and `pnpm check`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/hooks/useAdminLearning.ts src/GZCTF/ClientApp/src/components/admin/learning \
    src/GZCTF/ClientApp/src/pages/admin/library src/GZCTF/ClientApp/src/pages/admin/learning-paths \
    src/GZCTF/ClientApp/tests/e2e/admin-learning.spec.ts
  git commit -m "feat: add learning content administration"
  ```

## Task 7: Build import review and learning first navigation

- [ ] Add failing administrator cases for uploading the golden ZIP, seeing progress, opening the parity report, and locating the resulting draft route.
- [ ] Build `/admin/imports/Index.tsx` with package upload, batch status polling, counts, warnings, parity failures, and source mapping download.
- [ ] Update `AppNavbar.tsx` so primary navigation links to Learning, Posts, Account, and administrator learning sections. Keep old competition links until Wave 6 removes their routes, but hide them behind a temporary migration feature flag disabled by default.
- [ ] Update `useUser.tsx` logout invalidation to clear learning, challenge, import, and dashboard keys; remove assumptions that every protected key contains `game/`.
- [ ] Run all browser tests, `pnpm check`, and `pnpm build`.
- [ ] Commit:

  ```bash
  git add src/GZCTF/ClientApp/src/pages/admin/imports src/GZCTF/ClientApp/src/components/AppNavbar.tsx \
    src/GZCTF/ClientApp/src/hooks/useUser.tsx src/GZCTF/ClientApp/tests/e2e/admin-learning.spec.ts
  git commit -m "feat: add import review and learning navigation"
  ```

## Wave 4 verification

- [ ] Run the complete Playwright suite in Chinese and English.
- [ ] Capture traces for visitor, learner, and administrator journeys.
- [ ] Search rendered route pages for percentage and completed aggregate labels; expected: only account learning contains them.
- [ ] Complete a reused challenge from one route and verify both route outlines show an item checkmark and account records recalculate both route percentages.
- [ ] Verify keyboard navigation, focus order, form labels, loading states, empty states, and API error messages on every new page.
- [ ] Verify no browser response available to an anonymous context contains lesson Markdown, challenge body, hints, WP, attachment URL, or progress.
