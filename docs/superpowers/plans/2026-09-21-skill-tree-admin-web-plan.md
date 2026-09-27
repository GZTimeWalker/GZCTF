# Skill Tree Administrator Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 ST14–ST21，用可视化表单替代 Modules JSON，让管理员创建和发布技能树、维护全局类别、排序共享内容、合并或删除类别，并在题目和课节发布时选择或内联创建类别。

**Architecture:** 页面通过 ST13 生成的 `Api.ts` 调用后端；SWR hooks 统一缓存键和失效范围。技能树编辑器只管理类别引用和顺序，类别编辑器管理全局内容顺序，题目与课节共用一个发布弹窗，避免两套规则漂移。

**Tech Stack:** React 19, TypeScript 7, Vite, Mantine 9, SWR, react-router 8, i18next, Vitest, Playwright.

---

## 前置条件和产品边界

- ST13 已完成，`src/GZCTF/ClientApp/src/Api.ts` 含全部 SkillTree API。
- 管理员只填写名称、简介并选择预设图标，不填写 slug、UUID、locale 或 JSON。
- 技能树允许空发布。题目或课节发布时若所选树为空，必须在弹窗内创建类别。
- 技能树类别顺序属于树修订；类别内容顺序是全局共享顺序。
- 所有文案必须同时加入 `zh-CN` 和 `en-US`；其他旧语言由 i18next 回退到英语。

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/ClientApp/src/hooks/useSkillTreeAdmin.ts` | 管理端查询、写操作和缓存失效 |
| `src/GZCTF/ClientApp/src/utils/skillTreeAdmin.ts` | 图标映射、连续排序、错误码文案映射 |
| `src/GZCTF/ClientApp/src/utils/skillTreeAdmin.test.ts` | 纯函数单元测试 |
| `src/GZCTF/ClientApp/src/components/admin/skill-trees/PresetIconPicker.tsx` | 预设 CTF 图标选择器 |
| `src/GZCTF/ClientApp/src/components/admin/skill-trees/SkillTreeForm.tsx` | 名称、简介、图标表单 |
| `src/GZCTF/ClientApp/src/components/admin/skill-trees/CategoryPicker.tsx` | 已有类别选择和内联创建 |
| `src/GZCTF/ClientApp/src/components/admin/skill-trees/SortableCategoryList.tsx` | 当前树类别移除和排序 |
| `src/GZCTF/ClientApp/src/components/admin/skill-categories/CategoryContentList.tsx` | 全局内容排序 |
| `src/GZCTF/ClientApp/src/components/admin/skill-categories/CategoryMergeModal.tsx` | 类别合并 |
| `src/GZCTF/ClientApp/src/components/admin/shared/TypedDeleteModal.tsx` | 名称确认删除 |
| `src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx` | 题目和课节共用发布弹窗 |
| `src/GZCTF/ClientApp/src/pages/admin/skill-trees/Index.tsx` | 技能树列表和创建 |
| `src/GZCTF/ClientApp/src/pages/admin/skill-trees/[id].tsx` | 技能树编辑、预览、发布和删除 |
| `src/GZCTF/ClientApp/src/pages/admin/skill-categories/Index.tsx` | 全局类别列表和创建 |
| `src/GZCTF/ClientApp/src/pages/admin/skill-categories/[id].tsx` | 类别元数据、树引用、内容、合并和删除 |
| `src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx` | 接入共用发布弹窗 |
| `src/GZCTF/ClientApp/src/pages/admin/library/lessons/[id].tsx` | 接入共用发布弹窗 |
| `src/GZCTF/ClientApp/src/locales/zh-CN/skillTrees.json` | 简体中文文案 |
| `src/GZCTF/ClientApp/src/locales/en-US/skillTrees.json` | 英文文案和回退源 |
| `src/GZCTF/ClientApp/tests/e2e/admin-skill-trees.spec.ts` | 管理端浏览器验收 |

## Shared UI contracts

图标映射只接受后端允许键：

```ts
export const skillTreeIcons = {
  flag: '🚩',
  web: '🕸️',
  crypto: '🔐',
  pwn: '💣',
  brain: '🧠',
  ai: '🤖',
} as const

export type SkillTreeIconKey = keyof typeof skillTreeIcons

export const normalizeOrder = <T>(items: T[]): Array<T & { sortOrder: number }> =>
  items.map((item, sortOrder) => ({ ...item, sortOrder }))
```

管理端缓存键统一为：

```ts
export const skillTreeAdminKeys = {
  trees: '/api/admin/skill-trees',
  treeDraft: (id: string) => `/api/admin/skill-trees/${id}/draft`,
  treePreview: (id: string) => `/api/admin/skill-trees/${id}/draft/preview`,
  treeImpact: (id: string) => `/api/admin/skill-trees/${id}/delete-impact`,
  categories: '/api/admin/skill-categories',
  category: (id: string) => `/api/admin/skill-categories/${id}`,
  categoryImpact: (id: string) => `/api/admin/skill-categories/${id}/delete-impact`,
} as const
```

## Task 1: ST14 — Skill tree list and creation form

**Files:**
- Create: `src/GZCTF/ClientApp/src/utils/skillTreeAdmin.ts`
- Create: `src/GZCTF/ClientApp/src/utils/skillTreeAdmin.test.ts`
- Create: `src/GZCTF/ClientApp/src/hooks/useSkillTreeAdmin.ts`
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-trees/PresetIconPicker.tsx`
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-trees/SkillTreeForm.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/admin/skill-trees/Index.tsx`

- [x] **Step 1: Write failing utility tests**

```ts
import { describe, expect, it } from 'vitest'
import { normalizeOrder, skillTreeIcons } from './skillTreeAdmin'

describe('skill tree admin utilities', () => {
  it('exposes only backend supported icon keys', () => {
    expect(Object.keys(skillTreeIcons)).toEqual(['flag', 'web', 'crypto', 'pwn', 'brain', 'ai'])
  })

  it('normalizes order after movement or removal', () => {
    expect(normalizeOrder([{ id: 'b' }, { id: 'a' }])).toEqual([
      { id: 'b', sortOrder: 0 },
      { id: 'a', sortOrder: 1 },
    ])
  })
})
```

- [x] **Step 2: Run the red unit test**

```bash
cd src/GZCTF/ClientApp
pnpm vitest run src/utils/skillTreeAdmin.test.ts
```

Expected: module resolution fails because `skillTreeAdmin.ts` does not exist.

- [x] **Step 3: Implement utilities and hooks**

Implement the exact icon map and order helper above. `useSkillTreeAdmin` exports:

```ts
export const useAdminSkillTrees = () => Api.adminSkillTrees.adminSkillTreesList()
export const useAdminSkillCategories = () => Api.adminSkillCategories.adminSkillCategoriesList()
export const useAdminSkillTreeDraft = (id?: string) =>
  Api.adminSkillTrees.adminSkillTreesDraftDetail(id ?? '', { enabled: Boolean(id) })
```

Wrap generated mutation calls with functions `createTree`, `saveTreeDraft`, `publishTree`, `deleteTree`, `createCategory`, `saveCategory`, `sortCategoryContents`, `mergeCategories`, and `deleteCategory`. Each successful mutation calls `mutate` only for its list, detail, preview, impact, and affected public skill tree keys.

- [x] **Step 4: Implement preset icon picker**

Render six `UnstyledButton` items with Emoji, translated label, `aria-pressed`, and keyboard focus. The component accepts:

```ts
type PresetIconPickerProps = {
  value: SkillTreeIconKey
  onChange: (value: SkillTreeIconKey) => void
  disabled?: boolean
  error?: string
}
```

Do not render a free text icon input.

- [x] **Step 5: Implement tree creation page**

`SkillTreeForm` accepts `name`, `summary`, `iconKey`, validation errors and submit callback. Trim name on submit; name is required and limited by the generated schema constraint. `/admin/skill-trees` renders list cards plus a creation form. On success navigate to `/admin/skill-trees/{skillTreeId}`.

The page must contain no `Textarea` labelled JSON, no slug input, no locale picker, and no UUID input.

- [x] **Step 6: Run unit, type, and build checks**

```bash
pnpm test:unit
pnpm check
pnpm build
```

Expected: all pass.

- [x] **Step 7: Commit**

```bash
git add src/GZCTF/ClientApp/src/utils/skillTreeAdmin* \
  src/GZCTF/ClientApp/src/hooks/useSkillTreeAdmin.ts \
  src/GZCTF/ClientApp/src/components/admin/skill-trees \
  src/GZCTF/ClientApp/src/pages/admin/skill-trees/Index.tsx
git commit -m "feat: add skill tree admin creation"
```

## Task 2: ST15 — Visual skill tree editor and publication

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-trees/CategoryPicker.tsx`
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-trees/SortableCategoryList.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/admin/skill-trees/[id].tsx`
- Modify: `src/GZCTF/ClientApp/src/utils/skillTreeAdmin.test.ts`

- [x] **Step 1: Add failing reorder tests**

```ts
import { moveItem } from './skillTreeAdmin'

it('moves a category and emits continuous order', () => {
  expect(moveItem([{ id: 'a' }, { id: 'b' }, { id: 'c' }], 2, 0)).toEqual([
    { id: 'c', sortOrder: 0 },
    { id: 'a', sortOrder: 1 },
    { id: 'b', sortOrder: 2 },
  ])
})
```

- [x] **Step 2: Implement immutable reorder helper**

```ts
export const moveItem = <T>(items: T[], from: number, to: number) => {
  const copy = [...items]
  const [moved] = copy.splice(from, 1)
  copy.splice(to, 0, moved)
  return normalizeOrder(copy)
}
```

- [x] **Step 3: Build existing-category picker and inline create**

The picker searches active categories by name, disables categories already selected, and exposes a “创建类别” action. Inline form contains name, summary, preset icon. On create success it appends the returned category to local draft state without publishing the tree.

- [x] **Step 4: Build sortable category cards**

Use native pointer/keyboard controls so no dependency is added. Each card has move up, move down and remove buttons with translated `aria-label`. Removal only removes the draft reference and states that the global category remains.

- [x] **Step 5: Build tree editor page**

Load draft and all categories. Keep server `rowVersion` in form state. Save sends normalized category IDs and order. Preview opens a read-only modal built from the draft preview response. Publish saves first when dirty, then publishes with the returned row version. An empty category list is valid and displays the “内容建设中” preview.

When a request returns `skill_tree_revision_conflict`, keep local values, show a notification, and offer a single reload button. Do not silently overwrite the server draft.

- [x] **Step 6: Verify empty and populated publication**

Run the page against the local backend. Publish once with zero categories and once with two categories after changing their order. Refresh after each publish and verify the exact order remains.

- [x] **Step 7: Commit**

```bash
git add src/GZCTF/ClientApp/src/components/admin/skill-trees \
  'src/GZCTF/ClientApp/src/pages/admin/skill-trees/[id].tsx' \
  src/GZCTF/ClientApp/src/utils/skillTreeAdmin*
git commit -m "feat: add visual skill tree editor"
```

## Task 3: ST16 — Global category editor and content order

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-categories/CategoryContentList.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/admin/skill-categories/Index.tsx`
- Create: `src/GZCTF/ClientApp/src/pages/admin/skill-categories/[id].tsx`

- [x] **Step 1: Build category list and create form**

The list shows icon, name, tree count, challenge count, lesson count, and orphan badge. Creation uses the same name, summary, icon fields as a tree. An orphan category is a valid result and remains editable.

- [x] **Step 2: Build metadata and editable membership section**

The detail page shows every active tree with separate “已发布” and “草稿” badges and a membership checkbox. Saving membership sends each affected tree ID, desired inclusion state, and tree row version. The backend updates tree drafts only; after success, show affected trees as “有未发布变更” with preview and publish links. Editing metadata remains a separate category update using category `rowVersion`.

- [x] **Step 3: Build global content sorter**

Render a single ordered list combining challenges and lessons. Each row shows type, title, state and move controls. Save emits:

```ts
contents.map((item, sortOrder) => ({
  kind: item.kind,
  contentId: item.contentId,
  sortOrder,
}))
```

After save, invalidate category detail plus every referenced public and admin tree key returned by the response.

- [x] **Step 4: Verify shared order visually**

Create two skill trees referencing the same category, reorder a lesson above a challenge in the category page, open both tree previews, and verify both show the same content order without republishing either tree.

- [x] **Step 5: Commit**

```bash
git add src/GZCTF/ClientApp/src/components/admin/skill-categories \
  src/GZCTF/ClientApp/src/pages/admin/skill-categories
git commit -m "feat: manage shared skill categories"
```

## Task 4: ST17 — Category merge and impact-aware deletion

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/admin/skill-categories/CategoryMergeModal.tsx`
- Create: `src/GZCTF/ClientApp/src/components/admin/shared/TypedDeleteModal.tsx`
- Modify: `src/GZCTF/ClientApp/src/pages/admin/skill-categories/[id].tsx`

- [x] **Step 1: Implement reusable typed confirmation modal**

```ts
type TypedDeleteModalProps = {
  opened: boolean
  entityName: string
  title: string
  impactLines: string[]
  requiresTypedConfirmation: boolean
  loading: boolean
  onClose: () => void
  onConfirm: (confirmationName: string) => Promise<void>
}
```

The confirm button is disabled until the entered value exactly equals `entityName` when strong confirmation is required. It remains disabled while submitting.

- [x] **Step 2: Implement category merge modal**

The current category is the default survivor. The administrator selects one different active duplicate. Show the result rule: survivor content first, duplicate-only content appended, duplicate associations removed, progress retained. Submit both row versions.

- [x] **Step 3: Connect impact preview and delete**

Fetch impact only when the delete modal opens. Render published tree, draft tree, challenge and lesson counts. On success navigate to `/admin/skill-categories` and invalidate category/tree caches. On `409`, close neither modal nor page; show reload action.

- [x] **Step 4: Verify merge and delete flows**

Merge two categories sharing one challenge and each containing one unique item. Verify the survivor displays three unique items in the specified order. Delete a referenced category, type its full name, and verify it disappears from active tree previews while the content pages still exist.

- [x] **Step 5: Commit**

```bash
git add src/GZCTF/ClientApp/src/components/admin/shared/TypedDeleteModal.tsx \
  src/GZCTF/ClientApp/src/components/admin/skill-categories \
  'src/GZCTF/ClientApp/src/pages/admin/skill-categories/[id].tsx'
git commit -m "feat: add category merge and deletion flows"
```

## Task 5: ST18 — Challenge category selection and publication

**Files:**
- Create: `src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx`
- Modify: `src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx`
- Modify: `src/GZCTF/ClientApp/src/hooks/useAdminLearning.ts`

- [x] **Step 1: Define shared modal state**

```ts
export type ContentPublishSelection = {
  categoryIds: string[]
  inlineCategories: Array<{
    skillTreeId: string
    name: string
    summary: string
    iconKey: SkillTreeIconKey
  }>
}
```

The modal receives `kind`, `contentId`, `rowVersion`, initial category IDs and `onPublished`.

- [x] **Step 2: Implement tree-filtered multi-selection**

First select a skill tree. Show only categories belonging to its current draft or published revision. Selections from other trees remain visible as removable chips. The same global category appears once even when referenced by multiple selected trees.

- [x] **Step 3: Implement empty-tree inline category form**

When the selected tree has zero categories, replace the empty select with required name, optional summary, and preset icon. Saving the modal submits this object in `inlineCategories`; it does not issue a separate create request. This preserves backend transaction atomicity.

- [x] **Step 4: Connect challenge editor**

Saving the challenge remains a draft save. The “发布题目” button first saves dirty fields, then opens the modal using the returned row version. Successful publication closes the modal, refreshes edit response and category/tree caches, and displays a success notification.

- [x] **Step 5: Verify failure recovery**

Submit with no category and assert localized `content_category_required`. Select an orphan category through a stale view and assert localized `content_category_has_no_active_tree`. Modal selections remain intact after both failures.

- [x] **Step 6: Commit**

```bash
git add src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx \
  'src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx' \
  src/GZCTF/ClientApp/src/hooks/useAdminLearning.ts
git commit -m "feat: publish challenges into skill categories"
```

## Task 6: ST19 — Lesson draft and publication parity

**Files:**
- Modify: `src/GZCTF/ClientApp/src/pages/admin/library/lessons/[id].tsx`
- Modify: `src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx`

- [x] **Step 1: Connect lesson editor to the shared modal**

Use `kind="lesson"`. Draft save must succeed with zero categories. Publication uses the exact tree filter, category chips, inline creation, error mapping and cache invalidation as challenges.

- [x] **Step 2: Preserve localized lesson editing**

Keep current Chinese and English title/body inputs. The category selection is global and must not be nested inside locale tabs. Publication state badge uses `Draft`, `Published`, and `Retired` values from the generated API.

- [x] **Step 3: Verify challenge and lesson parity**

Publish one challenge and one lesson into the same category. Reopen both editors and assert the selected category is restored. Remove the category from the lesson, select another active category, republish, and verify the challenge association remains unchanged.

- [x] **Step 4: Commit**

```bash
git add 'src/GZCTF/ClientApp/src/pages/admin/library/lessons/[id].tsx' \
  src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx
git commit -m "feat: publish lessons into skill categories"
```

## Task 7: ST20 — Skill tree deletion flow

**Files:**
- Modify: `src/GZCTF/ClientApp/src/pages/admin/skill-trees/[id].tsx`
- Reuse: `src/GZCTF/ClientApp/src/components/admin/shared/TypedDeleteModal.tsx`

- [x] **Step 1: Add deletion section**

Load impact only after clicking delete. Show category, challenge, lesson, enrollment and publication counts. An empty unpublished draft uses ordinary confirmation; every tree with a published revision, category, content or enrollment requires the exact tree name.

- [x] **Step 2: Submit versioned delete**

Send `confirmationName` and impact response `rowVersion`, not a potentially stale editor value. On success navigate to list and invalidate admin list, public list, personal record, enrollment and deleted detail keys.

- [x] **Step 3: Verify current-tree effect**

Enroll a learner and make this tree current, then delete it. Verify the admin list hides it and `/api/my-learning` retains a historical record with `isDeleted=true` and no current tree.

- [x] **Step 4: Commit**

```bash
git add 'src/GZCTF/ClientApp/src/pages/admin/skill-trees/[id].tsx'
git commit -m "feat: add protected skill tree deletion"
```

## Task 8: ST21 — Translation and administrator browser suite

**Files:**
- Create: `src/GZCTF/ClientApp/src/locales/zh-CN/skillTrees.json`
- Create: `src/GZCTF/ClientApp/src/locales/en-US/skillTrees.json`
- Modify: `src/GZCTF/ClientApp/src/main.tsx`
- Create: `src/GZCTF/ClientApp/tests/e2e/admin-skill-trees.spec.ts`

- [x] **Step 1: Add complete namespace files**

Both JSON files must have the same key set. Include titles, labels, icon names, states, empty states, impact text, merge explanation, typed confirmation, publication errors, concurrency error, success notifications, and button accessible labels.

Register the namespace through the aggregate locale loader already used by `main.tsx`. Do not add a second i18next instance.

- [x] **Step 2: Add translation parity unit test**

```ts
import en from '../src/locales/en-US/skillTrees.json'
import zh from '../src/locales/zh-CN/skillTrees.json'

const flatten = (value: object, prefix = ''): string[] =>
  Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return child && typeof child === 'object' ? flatten(child as object, path) : [path]
  })

test('skill tree locale keys stay in parity', () => {
  expect(flatten(zh).sort()).toEqual(flatten(en).sort())
})
```

- [x] **Step 3: Add Playwright administrator journey**

The test logs in as Admin and performs this exact sequence:

1. Create and publish an empty skill tree.
2. Create two categories visually and add both to the tree.
3. Reorder categories and publish.
4. Create draft challenge, open publish modal, choose the tree and category, publish.
5. Create draft lesson and publish it into the same category.
6. Reorder category contents and verify both tree preview and category page.
7. Create a duplicate category and merge it.
8. Open category delete impact and cancel once before confirming.
9. Delete the skill tree using typed name.
10. Switch to Chinese and repeat list/create form assertions.

At each page attach listeners and fail on `pageerror`, console error, or response status `>= 500`.

- [x] **Step 4: Assert forbidden UI is absent**

```ts
await expect(page.getByLabel(/json|modules json/i)).toHaveCount(0)
await expect(page.getByLabel(/slug/i)).toHaveCount(0)
await expect(page.getByText(/skillTrees\.|learning:/)).toHaveCount(0)
```

- [x] **Step 5: Run the full frontend gate**

```bash
cd src/GZCTF/ClientApp
pnpm test:unit
pnpm check
pnpm build
pnpm playwright test tests/e2e/admin-skill-trees.spec.ts
```

Expected: every command passes in English and Simplified Chinese with clean browser logs.

- [x] **Step 6: Commit**

```bash
git add src/GZCTF/ClientApp/src/locales \
  src/GZCTF/ClientApp/src/main.tsx \
  src/GZCTF/ClientApp/tests/e2e/admin-skill-trees.spec.ts
git commit -m "test: cover skill tree administrator journeys"
```

## Wave ST3 completion checklist

- [x] 管理员创建技能树时只填写名称、简介并选择预设图标。
- [x] 技能树编辑页没有 Modules JSON、slug、UUID 或 locale 输入。
- [x] 空技能树与有类别技能树都可以预览和发布。
- [x] 类别可全局创建、作为孤儿保存、编辑、排序内容、合并和删除。
- [x] 同一类别的内容排序在所有引用树中立即一致。
- [x] 题目与课节共用一套发布弹窗和校验行为。
- [x] 空技能树发布内容时在同一弹窗输入类别并原子保存。
- [x] 所有强删除操作展示影响并校验完整名称。
- [x] 中文和英文 key 完全一致，页面不显示原始翻译 key。
- [x] 管理员 Playwright、单元测试、类型检查和生产构建全部通过。
