# Skill Tree Learner Cutover and Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 ST22–ST28，将学员入口和术语切换到技能树，保留旧书签重定向，退役旧 Learning API，并通过迁移、四类题型、大屏、浏览器、性能和生产镜像验收。

**Architecture:** 新学员页面只读取 ST2 的 SkillTree API；发现页和详情页不显示个人聚合进度，个人中心单独读取 `/api/my-learning`。旧 `/learn` 页面变为轻量重定向适配器，旧 Learning 表继续保留用于升级和审计，旧控制器与前端类型在重定向和数据一致性验证后移除。

**Tech Stack:** React 19, TypeScript 7, Vite, Mantine 9, SWR, react-router 8, ASP.NET Core 10, EF Core 10, PostgreSQL, xUnit, Playwright, Docker, Kubernetes.

---

## 前置条件和不可变部署合同

- ST13 和 ST21 已完成，生成客户端、管理端数据和翻译可用。
- 保留 `ChallengeProgress(UserId, ChallengeId)` 和 `LessonProgress(UserId, LessonId)` 作为全局完成真相。
- 保留静态附件、动态附件、静态容器、动态容器四种题型的运行行为。
- `Dockerfile` 入口保持 `dotnet GZCTF.dll`；应用端口保持 `8080`；健康检查和指标端口保持 `3000`。
- `GZCTF_` 配置前缀、PostgreSQL、Redis、存储、Docker 和 Kubernetes 配置保持兼容。
- 旧 Learning 数据表不在本阶段删除；只退役 HTTP 服务、应用服务和无用前端代码。

## Learner URL contract

| URL | Behavior |
|---|---|
| `/skill-trees` | 公开技能树发现页 |
| `/skill-trees/{id}` | 公开技能树详情，按类别显示内容 |
| `/skill-trees/{id}/{categoryId}/lesson/{contentId}` | 登录且已加入后阅读课节 |
| `/skill-trees/{id}/{categoryId}/challenge/{contentId}` | 登录且已加入后进入靶场 |
| `/account/learning` | 个人中心学习记录，唯一展示聚合进度的位置 |
| `/learn` | 301/客户端 replace 到 `/skill-trees` |
| `/learn/{slug}` | 通过迁移映射 replace 到 `/skill-trees/{id}` |
| `/learn/{slug}/{moduleId}/{itemId}` | 通过稳定迁移 ID replace 到新的内容工作区 |

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/ClientApp/src/hooks/useSkillTrees.ts` | 学员端查询、加入、当前树和缓存失效 |
| `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeCard.tsx` | 发现页卡片，不含进度 |
| `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeOutline.tsx` | 类别和内容列表 |
| `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeEnrollmentControls.tsx` | 加入、退出、设置当前 |
| `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeContentWorkspace.tsx` | 内容路由和访问状态 |
| `src/GZCTF/ClientApp/src/components/skill-trees/MySkillTreeRecord.tsx` | 个人中心进度和最近学习 |
| `src/GZCTF/ClientApp/src/pages/skill-trees/Index.tsx` | 发现页 |
| `src/GZCTF/ClientApp/src/pages/skill-trees/[id]/Index.tsx` | 技能树详情 |
| `src/GZCTF/ClientApp/src/pages/skill-trees/[id]/[categoryId]/[kind]/[contentId].tsx` | 内容工作区 |
| `src/GZCTF/ClientApp/src/pages/learn/Index.tsx` | `/learn` 兼容重定向 |
| `src/GZCTF/ClientApp/src/pages/learn/[slug]/Index.tsx` | 旧 slug 重定向 |
| `src/GZCTF/ClientApp/src/pages/learn/[slug]/[moduleId]/[itemId].tsx` | 旧深链重定向 |
| `src/GZCTF/ClientApp/src/components/AppNavbar.tsx` | “技能树”导航和树图标 |
| `src/GZCTF/ClientApp/src/pages/account/Learning.tsx` | 个人中心学习记录入口 |
| `src/GZCTF/Features/SkillTrees/Application/LearningRedirectService.cs` | 旧 slug 和深链目标解析 |
| `src/GZCTF/Features/SkillTrees/Api/LearningRedirectsController.cs` | 只读重定向 API |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/LearningRedirectTests.cs` | 旧地址映射 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeParityTests.cs` | 迁移计数、进度和运行模式一致性 |
| `src/GZCTF/ClientApp/tests/e2e/skill-trees.spec.ts` | 访客和学员浏览器流程 |
| `src/GZCTF/ClientApp/tests/e2e/skill-tree-card.spec.ts` | 发现页公开信息和无进度契约 |
| `src/GZCTF/ClientApp/tests/e2e/skill-tree-redirects.spec.ts` | 旧地址重定向 |
| `scripts/verify-skill-tree-release.sh` | 可重复执行的生产镜像验收命令 |

## Task 1: ST22 — Skill tree discovery page

**Files:**
- Create: `src/GZCTF/ClientApp/src/hooks/useSkillTrees.ts`
- Create: `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeCard.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/skill-trees/Index.tsx`
- Create: `src/GZCTF/ClientApp/tests/e2e/skill-tree-card.spec.ts`

- [ ] **Step 1: Add failing card contract test**

```ts
import { expect, test } from '@playwright/test'

test('shows public counts and never renders aggregate progress', async ({ page }) => {
  await page.route('**/api/skill-trees', route => route.fulfill({
    contentType: 'application/json',
    body: JSON.stringify([{
      skillTreeId: '01990000-0000-7000-8000-000000000001',
      name: 'Web 工程师', summary: '从 HTTP 到漏洞利用', iconKey: 'web',
      categoryCount: 3, challengeCount: 12, lessonCount: 6,
    }]),
  }))
  await page.goto('/skill-trees')
  await expect(page.getByText('Web 工程师')).toBeVisible()
  await expect(page.getByText(/3.*类别/)).toBeVisible()
  await expect(page.getByText(/12.*题目/)).toBeVisible()
  await expect(page.getByText(/6.*课节/)).toBeVisible()
  await expect(page.getByText(/%|路线进度|完成模块|完成课时/i)).toHaveCount(0)
})
```

- [ ] **Step 2: Implement learner hooks**

```ts
export const skillTreeKeys = {
  list: '/api/skill-trees',
  detail: (id: string) => `/api/skill-trees/${id}`,
  enrollments: '/api/skill-tree-enrollments',
  record: '/api/my-learning',
} as const

export const useSkillTrees = () => Api.skillTrees.skillTreesList()
export const useSkillTree = (id?: string) =>
  Api.skillTrees.skillTreesDetail(id ?? '', { enabled: Boolean(id) })
```

Mutation helpers invalidate list only when counts or visibility change; enrollment mutations invalidate enrollments, detail controls and personal record, without refetching static challenge data.

- [ ] **Step 3: Implement cards**

Map `IconKey` through the fixed icon map. Card displays icon, name, summary, category count, challenge count and lesson count, and links by ID to `/skill-trees/{id}`. It must not read auth state or progress.

- [ ] **Step 4: Implement discovery states**

`/skill-trees` handles loading skeleton, network error with retry, no published trees, and responsive card grid. A published empty tree appears normally with all counts zero.

- [ ] **Step 5: Run frontend checks and commit**

```bash
cd src/GZCTF/ClientApp
pnpm test:unit
pnpm check
pnpm build
git add src/hooks/useSkillTrees.ts src/components/skill-trees src/pages/skill-trees/Index.tsx
git commit -m "feat: add skill tree discovery page"
```

## Task 2: ST23 — Tree detail and content workspace

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeOutline.tsx`
- Create: `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeContentWorkspace.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/skill-trees/[id]/Index.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/skill-trees/[id]/[categoryId]/[kind]/[contentId].tsx`
- Modify: `src/GZCTF/ClientApp/src/components/learning/ChallengeWorkspace.tsx`
- Modify: `src/GZCTF/ClientApp/src/components/learning/LessonWorkspace.tsx`

- [ ] **Step 1: Implement detail outline**

Render tree icon, name and summary, followed by categories in API order and content in each category's global order. Content rows show title, kind, expected minutes and challenge difficulty. A tree with no categories renders the translated “内容建设中” state.

Do not show route completion percentage, completed module count, completed lesson count, progress bar, or leaderboard.

- [ ] **Step 2: Build deterministic content links**

```ts
const contentHref = (
  treeId: string,
  categoryId: string,
  item: { kind: string; contentId: string },
) => `/skill-trees/${treeId}/${categoryId}/${item.kind}/${item.contentId}`
```

Validate `kind` at render and route time; any value other than `challenge` or `lesson` displays the 404 page.

- [ ] **Step 3: Reuse existing workspaces**

The route wrapper checks authentication and enrollment before requesting protected bodies or starting instances. For `challenge`, render `ChallengeWorkspace` with `challengeId`. For `lesson`, render `LessonWorkspace` with `lessonId`. Add optional `backHref`, `previousHref`, and `nextHref` props rather than duplicating runtime/help/submission logic.

- [ ] **Step 4: Calculate previous and next links within the displayed tree**

Flatten category contents in category order and content order. Locate the current tuple `(categoryId, kind, contentId)`. Previous and next use adjacent tuples. Shared content reached from another tree stays in that tree's navigation context.

- [ ] **Step 5: Verify shared category behavior**

Open two trees sharing a category. Assert names and global content order match while category placement relative to other categories follows each tree's revision order.

- [ ] **Step 6: Run checks and commit**

```bash
git add src/GZCTF/ClientApp/src/components/skill-trees \
  src/GZCTF/ClientApp/src/components/learning/ChallengeWorkspace.tsx \
  src/GZCTF/ClientApp/src/components/learning/LessonWorkspace.tsx \
  src/GZCTF/ClientApp/src/pages/skill-trees
git commit -m "feat: add skill tree detail and workspaces"
```

## Task 3: ST24 — Enrollment, current tree, and personal records

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/skill-trees/SkillTreeEnrollmentControls.tsx`
- Create: `src/GZCTF/ClientApp/src/components/skill-trees/MySkillTreeRecord.tsx`
- Modify: `src/GZCTF/ClientApp/src/pages/skill-trees/[id]/Index.tsx`
- Modify: `src/GZCTF/ClientApp/src/pages/account/Learning.tsx`
- Modify: `src/GZCTF/ClientApp/src/hooks/useSkillTrees.ts`

- [ ] **Step 1: Implement enrollment controls**

Anonymous visitors see a login link. Logged-in users see one of: join, leave, set current, or current badge. Disable every control during mutation. Leaving a current tree shows an ordinary confirmation explaining that no replacement is selected automatically.

- [ ] **Step 2: Keep progress out of discovery and detail**

The detail page may show per-content solved/completed markers only when the API already returns them for an authenticated learner. It must not calculate or show the aggregate strings `38%`, `3 / 8`, `16 / 42`, route progress, completed categories, or completed lessons.

- [ ] **Step 3: Build personal learning record**

Only `/account/learning` renders:

- Current skill tree.
- Each joined or historical tree's percentage.
- Completed categories over total categories.
- Completed lessons over total lessons.
- Solved challenges over total challenges.
- Recent learning activity.

Compute the percentage from distinct challenge and lesson totals returned by the API; when total content is zero, render `0%` rather than divide by zero. Deleted trees display a historical badge and no action controls.

- [ ] **Step 4: Verify migrated enrollments**

Seed an old Learning enrollment before ST1 backfill, start the upgraded app, sign in, and assert the matching skill tree is joined and current. Complete one shared challenge and assert both joined tree records update while the database still contains one progress row.

- [ ] **Step 5: Commit**

```bash
git add src/GZCTF/ClientApp/src/components/skill-trees \
  'src/GZCTF/ClientApp/src/pages/skill-trees/[id]/Index.tsx' \
  src/GZCTF/ClientApp/src/pages/account/Learning.tsx \
  src/GZCTF/ClientApp/src/hooks/useSkillTrees.ts
git commit -m "feat: add skill tree enrollment and records"
```

## Task 4: ST25 — Navigation, terminology, and legacy redirects

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/LearningRedirectService.cs`
- Create: `src/GZCTF/Features/SkillTrees/Api/LearningRedirectsController.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/LearningRedirectTests.cs`
- Modify: `src/GZCTF/ClientApp/src/components/AppNavbar.tsx`
- Replace: `src/GZCTF/ClientApp/src/pages/learn/Index.tsx`
- Replace: `src/GZCTF/ClientApp/src/pages/learn/[slug]/Index.tsx`
- Replace: `src/GZCTF/ClientApp/src/pages/learn/[slug]/[moduleId]/[itemId].tsx`
- Modify: `src/GZCTF/ClientApp/src/locales/zh-CN/skillTrees.json`
- Modify: `src/GZCTF/ClientApp/src/locales/en-US/skillTrees.json`

- [ ] **Step 1: Add failing redirect integration tests**

```csharp
[Fact]
public async Task Old_slug_redirects_to_stable_skill_tree_id()
{
    var response = await client.GetFromJsonAsync<LearningRedirectResponse>(
        $"/api/skill-tree-redirects/{oldSlug}");
    Assert.Equal($"/skill-trees/{skillTreeId}", response!.TargetPath);
}

[Fact]
public async Task Old_deep_link_uses_migrated_module_and_item_ids()
{
    var response = await client.GetFromJsonAsync<LearningRedirectResponse>(
        $"/api/skill-tree-redirects/{oldSlug}?moduleId={oldModuleId}&itemId={oldItemId}");
    Assert.Equal(
        $"/skill-trees/{skillTreeId}/{oldModuleId}/challenge/{challengeId}",
        response!.TargetPath);
}
```

Add lesson deep link, unknown slug, mismatched module/item, and deleted tree cases. Unknown or invalid mappings return `404`, never guess a target.

- [ ] **Step 2: Implement redirect lookup**

The service queries `LearningPathRedirects` by exact old slug. Without IDs it returns the tree path. With both IDs it verifies `SkillCategory.Id == moduleId`, `CategoryContent.Id == itemId`, resolves content kind and target ID, and returns the new workspace path. Use retained published audit references so an old bookmark can resolve even after current category changes; return `404` when the target tree is deleted.

- [ ] **Step 3: Replace old pages with route adapters**

`/learn` returns `<Navigate replace to="/skill-trees" />`. Slug routes fetch the redirect API, render a small loading state, then call `navigate(targetPath, { replace: true })`. On `404` render the normal not-found page.

- [ ] **Step 4: Rename navigation**

Replace `mdiFlagOutline` for the main learning entry with `mdiFileTreeOutline`, label it from `skillTrees:navigation.title`, and link to `/skill-trees`. Change the admin link to `/admin/skill-trees`. The account route may remain `/account/learning`, but visible copy must use skill tree terminology.

- [ ] **Step 5: Search visible old terminology**

```bash
rg -n '学习路线|学习模块|Learning Path|Learning Module|/admin/learning-paths' \
  src/GZCTF/ClientApp/src --glob '*.tsx' --glob '*.ts' --glob '*.json'
```

Expected: only migration notes, compatibility redirect keys, or intentionally historical text remain.

- [ ] **Step 6: Run tests and commit**

```bash
git add src/GZCTF/Features/SkillTrees/Application/LearningRedirectService.cs \
  src/GZCTF/Features/SkillTrees/Api/LearningRedirectsController.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/LearningRedirectTests.cs \
  src/GZCTF/ClientApp/src/components/AppNavbar.tsx \
  src/GZCTF/ClientApp/src/pages/learn \
  src/GZCTF/ClientApp/src/locales
git commit -m "feat: cut navigation over to skill trees"
```

## Task 5: ST26 — Retire old Learning HTTP and frontend implementation

**Files:**
- Delete: `src/GZCTF/Features/LearningPaths/Api/AdminLearningPathsController.cs`
- Delete: `src/GZCTF/Features/LearningPaths/Api/LearningPathsController.cs`
- Delete: `src/GZCTF/Features/LearningPaths/Api/EnrollmentsController.cs`
- Delete or reduce: `src/GZCTF/Features/LearningPaths/Application/LearningPathService.cs`
- Delete or reduce: `src/GZCTF/Features/LearningPaths/Application/EnrollmentService.cs`
- Delete: `src/GZCTF/ClientApp/src/hooks/useLearning.ts`
- Delete: `src/GZCTF/ClientApp/src/hooks/useAdminLearning.ts` after remaining library helpers move to named hooks
- Delete: `src/GZCTF/ClientApp/src/components/learning/RouteCard.tsx`
- Delete: `src/GZCTF/ClientApp/src/components/learning/RouteOutline.tsx`
- Delete: `src/GZCTF/ClientApp/src/components/learning/EnrollmentControls.tsx`
- Replace: `src/GZCTF/ClientApp/src/components/learning/LearningRecord.tsx`
- Delete: `src/GZCTF/ClientApp/src/pages/admin/learning-paths/**`
- Regenerate: `src/GZCTF/ClientApp/src/Api.ts`

- [ ] **Step 1: Add a failing legacy-surface test**

Update `LegacySurfaceTests` with exact assertions:

```csharp
[Theory]
[InlineData("/api/learning-paths")]
[InlineData("/api/admin/learning-paths")]
public async Task Retired_learning_apis_are_not_routable(string route)
{
    var response = await client.GetAsync(route);
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

Keep `/api/skill-tree-redirects/{slug}` covered and available.

- [ ] **Step 2: Remove HTTP controllers and unused services**

Delete only code with no caller after the skill tree cutover. Retain `LearningPathModels.cs`, EF mapping, source tables, `Lesson`, `LessonProgress`, migration code and backfill inputs. If lesson content authorization still lives in an old service, move it to `SkillTreeEnrollmentService` before deleting the service.

- [ ] **Step 3: Remove old frontend pages and helpers**

Keep the three `/learn` redirect adapters. Remove old route cards, module outline, LearningPath hooks and admin JSON editor. Move reusable challenge/lesson helpers to `useChallengeLibraryAdmin.ts` so no SkillTree page imports `useAdminLearning.ts`.

- [ ] **Step 4: Regenerate API and prove removal**

Start the backend, run `pnpm genapi`, then:

```bash
rg -n '/api/learning-paths|AdminLearningPaths|LearningPathSummaryResponse|LearningModuleResponse' \
  src/GZCTF src/GZCTF/ClientApp/src
```

Expected: matches exist only in migration/backfill entities, retained database mapping, redirect compatibility tests, and historical docs. `Api.ts` has no old HTTP modules.

- [ ] **Step 5: Run build and tests before commit**

Run backend build, all Learning/SkillTree integration tests, frontend unit tests, typecheck and build. Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add -A src/GZCTF/Features/LearningPaths \
  src/GZCTF/Features/SkillTrees \
  src/GZCTF/ClientApp/src \
  src/GZCTF.Integration.Test/Tests/Decommission/LegacySurfaceTests.cs
git commit -m "refactor: retire learning path application surface"
```

## Task 6: ST27 — Parity, browser, dashboard, and performance gates

**Files:**
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeParityTests.cs`
- Create: `src/GZCTF/ClientApp/tests/e2e/skill-trees.spec.ts`
- Create: `src/GZCTF/ClientApp/tests/e2e/skill-tree-redirects.spec.ts`
- Modify: `src/GZCTF.Integration.Test/Tests/Runtime/ChallengeModeContractTests.cs`
- Modify: `src/GZCTF.Integration.Test/Tests/Dashboard/DashboardProjectionTests.cs`

- [ ] **Step 1: Add upgraded-database parity test**

Seed the last pre-skill-tree schema with paths, same-name modules, lessons, all four challenge modes, enrollments, current selection, lesson progress and challenge progress. Apply current migrations and backfill twice. Assert:

```csharp
Assert.Equal(sourcePathCount, await db.SkillTrees.CountAsync(x => sourcePathIds.Contains(x.Id)));
Assert.Equal(sourceModuleCount, await db.SkillCategories.CountAsync(x => sourceModuleIds.Contains(x.Id)));
Assert.Equal(sourceItemCount, await db.CategoryContents.CountAsync(x => sourceItemIds.Contains(x.Id)));
Assert.Equal(sourceEnrollmentCount, await db.SkillTreeEnrollments.CountAsync(x => sourceEnrollmentIds.Contains(x.Id)));
Assert.Equal(sourceChallengeProgressCount, await db.ChallengeProgress.CountAsync());
Assert.Equal(sourceLessonProgressCount, await db.LessonProgress.CountAsync());
Assert.Equal(sourceModuleCount, await db.SkillCategories.CountAsync(x => x.Name == duplicatedModuleName));
```

The final assertion proves same-name modules remain independent.

- [ ] **Step 2: Prove four challenge modes through skill tree links**

For static attachment, dynamic attachment, static container and dynamic container:

1. Publish the challenge into an active category.
2. Enroll the learner.
3. Fetch it through the skill tree workspace route.
4. Download or allocate its resource.
5. Submit the correct Flag.
6. Assert one `ChallengeProgress` row.
7. Re-submit and assert the count remains one.

Reuse `ChallengeModeFixtures`; do not duplicate container implementation in the test.

- [ ] **Step 3: Prove ZIP import behavior**

Run the existing legacy ZIP import twice. Assert every imported game creates one draft tree, each source category creates an independent category, challenges retain source mapping/runtime/flags/attachment hashes, and no tree, category or content association duplicates.

- [ ] **Step 4: Prove dashboard totals remain global**

Solve one challenge referenced by two categories and two trees. Assert the large-screen cumulative series and Top leaderboard count the member's distinct solved challenge once. Filter by cohort/year and assert the same global progress source is used; skill tree association must not multiply dashboard totals.

- [ ] **Step 5: Add visitor and learner Playwright journey**

The suite performs:

1. Anonymous visitor opens discovery and an empty published tree.
2. Assert cards and detail have no aggregate progress strings.
3. Sign in, join two trees and set one current.
4. Open a Markdown lesson and mark complete.
5. Start a challenge, view a hint and WP, submit a Flag.
6. Open personal records and verify both shared references reflect completion.
7. Switch Chinese/English and assert no raw key appears.
8. Open old root, slug and deep links and assert final new URLs.

Fail on console errors, page errors and responses `>= 500`.

- [ ] **Step 6: Add bounded-query performance assertion**

Use EF command interception around `GET /api/skill-trees/{id}`. Seed 1 category and record command count, then seed 50 categories with 20 items each and repeat. Assert the latter count is no more than the baseline plus one. Assert response size grows with content but no protected body or repeated category graph is serialized.

- [ ] **Step 7: Run all release behavior suites**

```bash
docker run --rm \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet test GZCTF.Test/GZCTF.Test.csproj -c Debug

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

cd src/GZCTF/ClientApp
pnpm test:unit
pnpm check
pnpm build
pnpm test:e2e
```

Expected: all pass.

- [ ] **Step 8: Commit evidence tests**

```bash
git add src/GZCTF.Integration.Test/Tests/SkillTrees \
  src/GZCTF.Integration.Test/Tests/Runtime/ChallengeModeContractTests.cs \
  src/GZCTF.Integration.Test/Tests/Dashboard \
  src/GZCTF/ClientApp/tests/e2e
git commit -m "test: prove skill tree behavior parity"
```

## Task 7: ST28 — Production image and deployment contract

**Files:**
- Create: `scripts/verify-skill-tree-release.sh`
- Modify only deployment files when a verified regression requires it; expected result is no deployment contract change.

- [ ] **Step 1: Add executable release script**

```bash
#!/usr/bin/env bash
set -euo pipefail

image="gzctf-skill-tree:verification"
network="gzctf-skill-tree-verification"
db="gzctf-skill-tree-db"
redis="gzctf-skill-tree-redis"
app="gzctf-skill-tree-app"

cleanup() {
  docker rm -f "$app" "$db" "$redis" >/dev/null 2>&1 || true
  docker network rm "$network" >/dev/null 2>&1 || true
}
trap cleanup EXIT

: "${SixLaborsLicenseKey:?SixLaborsLicenseKey is required for the Release build}"
docker run --rm \
  -e SixLaborsLicenseKey \
  -v "$PWD:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src/GZCTF \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine \
  dotnet publish GZCTF.csproj -c Release -o publish/linux/amd64 \
    -r linux-x64 --no-self-contained /p:PublishReadyToRun=true
docker build --build-arg TARGETPLATFORM=linux/amd64 \
  -t "$image" -f src/GZCTF/Dockerfile src/GZCTF
docker network create "$network"
docker run -d --name "$db" --network "$network" \
  -e POSTGRES_DB=gzctf -e POSTGRES_USER=gzctf -e POSTGRES_PASSWORD=gzctf \
  postgres:15-alpine
docker run -d --name "$redis" --network "$network" redis:7-alpine
docker run -d --name "$app" --network "$network" -p 58080:8080 -p 53000:3000 \
  -e GZCTF_ConnectionStrings__Database='Host=gzctf-skill-tree-db;Database=gzctf;Username=gzctf;Password=gzctf' \
  -e GZCTF_ConnectionStrings__RedisCache='gzctf-skill-tree-redis:6379' \
  -e GZCTF_ConnectionStrings__Storage='disk://path=./files' \
  -e GZCTF_XorKey='skill-tree-release-verification' \
  -e YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE=true \
  "$image"

for _ in $(seq 1 60); do
  curl -fsS http://localhost:53000/healthz >/dev/null && break
  sleep 2
done

curl -fsS http://localhost:53000/healthz
curl -fsS http://localhost:58080/api/skill-trees | jq -e 'type == "array"'
curl -fsS http://localhost:53000/metrics >/dev/null
docker inspect "$app" --format '{{json .Config.Entrypoint}} {{json .Config.Cmd}}'
```

Adjust only the existing health path if repository configuration proves a different current path; do not invent a new endpoint for this script.

- [ ] **Step 2: Verify image metadata**

Inspect entrypoint, exposed ports, non-root user policy if present, image size and healthcheck. Confirm the command still launches `dotnet GZCTF.dll`, application responds on 8080, and metrics/health responds on 3000.

- [ ] **Step 3: Verify persistent upgrade**

Start the previous released image against a named PostgreSQL volume, seed/import representative data, stop only the app, then start the new image against the same volume. Assert accounts and admin roles remain, skill tree backfill succeeds, learning progress follows the approved migration behavior, and a second restart changes no migrated counts.

- [ ] **Step 4: Verify storage and runtime providers**

Run one disk storage attachment flow and one MinIO/S3 integration flow. Run static and dynamic containers with Docker. Run the existing Kubernetes integration mode for allocation, cleanup and traffic routing. Redis-enabled and Redis-absent supported configurations must both start according to existing configuration semantics.

- [ ] **Step 5: Inspect compose and Kubernetes diff**

```bash
git diff -- Dockerfile docker-compose.yml configs charts manifests .github/workflows
```

Expected: no port, entrypoint, environment prefix, volume, readiness, storage, Redis, Docker socket or Kubernetes service contract changed solely for the skill tree feature.

- [ ] **Step 6: Run final repository checks**

```bash
git diff --check
rg -n '/api/learning-paths|admin/learning-paths|LearningPathSummaryResponse|LearningModuleResponse' \
  src/GZCTF src/GZCTF/ClientApp/src
```

Expected search results are limited to migration/backfill, retained EF entities, redirect compatibility and historical tests.

- [ ] **Step 7: Commit release verification**

```bash
chmod +x scripts/verify-skill-tree-release.sh
git add scripts/verify-skill-tree-release.sh
git commit -m "test: verify skill tree release image"
```

## Wave ST4 completion checklist

- [ ] `/skill-trees` 和 `/skill-trees/{id}` 对访客可见且不显示聚合进度。
- [ ] 课节和题目工作区要求登录及加入技能树，并复用已有运行、提示和 WP 行为。
- [ ] 学员可以加入多棵树并设置一棵当前树。
- [ ] 聚合完成率、类别、课节和解题统计只在个人中心学习记录显示。
- [ ] `/learn`、旧 slug 和旧深链全部正确 replace 到新地址。
- [ ] 可见术语和导航统一为技能树与类别。
- [ ] 旧 Learning HTTP API 和前端生成类型已退役，数据库表仍保留。
- [ ] 旧数据库升级和重复启动幂等，迁移计数与关联一致。
- [ ] 静态附件、动态附件、静态容器、动态容器全部通过。
- [ ] ZIP 导入重复执行不产生重复树、类别或内容关联。
- [ ] 大屏累计曲线和排行榜按不同已解题统计，不因共享类别重复计数。
- [ ] 技能树详情无 N+1 查询和无界实体序列化。
- [ ] 中文、英文、访客、学员、管理员和旧地址浏览器流程全部通过。
- [ ] 生产镜像入口、端口、健康、Redis、存储、Docker 和 Kubernetes 合同保持不变。
