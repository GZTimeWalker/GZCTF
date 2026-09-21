# Skill Tree Application and API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 ST06–ST13，为技能树、共享类别、题目和课节发布、加入学习、个人记录、合并与软删除提供稳定 API，并从 OpenAPI 重新生成 TypeScript 客户端。

**Architecture:** 读取接口使用 `AsNoTracking` 投影和批量内容查询；写接口由独立应用服务承担事务、行版本校验和缓存失效。技能树修订只保存有序类别引用，类别内容实时共享；现有 Learning API 保留到 ST26。

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL, xUnit, Testcontainers, OpenAPI, swagger-typescript-api.

---

## 前置条件和边界

- ST01–ST05 已提交，`SkillTree`、`SkillTreeRevision`、`SkillCategory`、`SkillTreeCategoryRef`、`CategoryContent`、`SkillTreeEnrollment` 和 `LearningPathRedirect` 已存在。
- 本阶段不改前端页面，不删除 `Features/LearningPaths`，不修改容器入口或端口。
- 所有时间使用 UTC，所有写接口接受 `CancellationToken`。
- `RowVersion` 在 JSON 中使用无符号整数。缺失或不匹配均返回 `409` 和稳定错误码。
- 公开接口不返回 Flag、提示正文、WP 正文、未发布课节正文、用户字段或删除实体。

## File map

| File | Responsibility |
|---|---|
| `src/GZCTF/Features/SkillTrees/Application/SkillTreeContracts.cs` | 命令、响应和稳定错误码 |
| `src/GZCTF/Features/SkillTrees/Application/SkillTreeQueryService.cs` | 公开发现页和详情投影 |
| `src/GZCTF/Features/SkillTrees/Application/AdminSkillTreeService.cs` | 创建、草稿、预览、发布、影响预览、软删除 |
| `src/GZCTF/Features/SkillTrees/Application/SkillCategoryService.cs` | 类别 CRUD、树关联、内容排序、合并、删除 |
| `src/GZCTF/Features/SkillTrees/Application/SkillTreeEnrollmentService.cs` | 加入、退出、当前技能树和个人记录 |
| `src/GZCTF/Features/SkillTrees/Application/ContentPublicationService.cs` | 题目和课节类别绑定、内联类别创建、发布校验 |
| `src/GZCTF/Features/SkillTrees/Application/SkillTreeExceptions.cs` | 可映射为 ProblemDetails 的领域异常 |
| `src/GZCTF/Features/SkillTrees/Api/SkillTreesController.cs` | 匿名技能树 API |
| `src/GZCTF/Features/SkillTrees/Api/AdminSkillTreesController.cs` | 管理员技能树 API |
| `src/GZCTF/Features/SkillTrees/Api/AdminSkillCategoriesController.cs` | 管理员类别 API |
| `src/GZCTF/Features/SkillTrees/Api/SkillTreeEnrollmentsController.cs` | 登录用户加入和当前技能树 API |
| `src/GZCTF/Features/SkillTrees/Api/MyLearningController.cs` | 个人学习记录 API |
| `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs` | 调用统一内容发布服务 |
| `src/GZCTF/Features/ChallengeLibrary/Api/AdminChallengesController.cs` | 题目发布接口 |
| `src/GZCTF/Features/ChallengeLibrary/Api/AdminLessonsController.cs` | 课节发布接口 |
| `src/GZCTF/Extensions/Startup/ServicesExtension.cs` | 注册应用服务 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeQueryTests.cs` | 公开投影和匿名边界 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreePublishingTests.cs` | 草稿、空发布、并发和原子切换 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillCategoryTests.cs` | 共享、排序、删除和合并 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/ContentPublicationTests.cs` | 题目和课节发布规则 |
| `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeEnrollmentTests.cs` | 加入、当前树、删除和个人记录 |
| `src/GZCTF/ClientApp/src/Api.ts` | OpenAPI 生成结果，禁止手工编辑 |

## API contract

| Method | Route | Authentication | Result |
|---|---|---|---|
| GET | `/api/skill-trees` | Anonymous | 已发布且未删除的技能树摘要 |
| GET | `/api/skill-trees/{id}` | Anonymous | 已发布类别及已发布内容简介 |
| GET | `/api/admin/skill-trees` | Admin | 管理列表 |
| POST | `/api/admin/skill-trees` | Admin | 创建空草稿技能树 |
| GET | `/api/admin/skill-trees/{id}/draft` | Admin | 获取或由发布版本复制草稿 |
| PUT | `/api/admin/skill-trees/{id}/draft` | Admin | 更新基本信息和类别顺序 |
| GET | `/api/admin/skill-trees/{id}/draft/preview` | Admin | 草稿预览 |
| POST | `/api/admin/skill-trees/{id}/publish` | Admin | 原子发布，允许空类别 |
| GET | `/api/admin/skill-trees/{id}/delete-impact` | Admin | 删除影响统计 |
| DELETE | `/api/admin/skill-trees/{id}` | Admin | 名称和行版本确认后软删除 |
| GET/POST | `/api/admin/skill-categories` | Admin | 列表和创建 |
| GET/PUT | `/api/admin/skill-categories/{id}` | Admin | 详情和编辑 |
| PUT | `/api/admin/skill-categories/{id}/tree-memberships` | Admin | 在各技能树草稿中加入或移出类别 |
| PUT | `/api/admin/skill-categories/{id}/contents` | Admin | 全局内容排序 |
| GET | `/api/admin/skill-categories/{id}/delete-impact` | Admin | 类别删除影响统计 |
| DELETE | `/api/admin/skill-categories/{id}` | Admin | 类别软删除 |
| POST | `/api/admin/skill-categories/merge` | Admin | 事务合并 |
| GET | `/api/skill-tree-enrollments` | User | 当前用户的加入记录 |
| POST/DELETE | `/api/skill-tree-enrollments/{id}` | User | 加入或退出 |
| PUT | `/api/skill-tree-enrollments/{id}/current` | User | 设置当前技能树 |
| GET | `/api/my-learning` | User | 个人学习记录 |
| POST | `/api/admin/challenges/{id}/publish` | Admin | 绑定类别并发布题目 |
| POST | `/api/admin/lessons/{id}/publish` | Admin | 绑定类别并发布课节 |

稳定错误码定义：

```csharp
public static class SkillTreeProblemCodes
{
    public const string NotFound = "skill_tree_not_found";
    public const string CategoryNotFound = "skill_category_not_found";
    public const string RevisionConflict = "skill_tree_revision_conflict";
    public const string InvalidIcon = "skill_tree_invalid_icon";
    public const string InvalidCategoryOrder = "skill_tree_invalid_category_order";
    public const string ConfirmationMismatch = "skill_tree_confirmation_mismatch";
    public const string ContentCategoryRequired = "content_category_required";
    public const string CategoryHasNoTree = "content_category_has_no_active_tree";
    public const string MergeConflict = "skill_category_merge_conflict";
}
```

错误响应使用：

```json
{
  "type": "https://gzctf.dev/problems/skill-tree-revision-conflict",
  "title": "The skill tree was changed by another administrator.",
  "status": 409,
  "code": "skill_tree_revision_conflict"
}
```

## Task 1: ST06 — Freeze public query contracts

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/SkillTreeContracts.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeQueryTests.cs`

- [x] **Step 1: Add failing anonymous boundary tests**

Seed one empty published tree, one populated published tree, one draft tree, one deleted tree, a published challenge, a draft lesson, and a published lesson. Assert:

```csharp
[Fact]
public async Task Anonymous_list_returns_only_active_published_trees_without_progress()
{
    var response = await client.GetFromJsonAsync<SkillTreeSummaryResponse[]>("/api/skill-trees");
    Assert.All(response!, tree =>
    {
        Assert.DoesNotContain("progress", JsonSerializer.Serialize(tree), StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(Guid.Empty, tree.SkillTreeId);
    });
    Assert.Contains(response!, tree => tree.Name == "Empty published tree" && tree.CategoryCount == 0);
    Assert.DoesNotContain(response!, tree => tree.Name is "Draft tree" or "Deleted tree");
}

[Fact]
public async Task Anonymous_detail_excludes_draft_content_and_protected_fields()
{
    var json = await client.GetStringAsync($"/api/skill-trees/{publishedTreeId}");
    Assert.DoesNotContain("\"flags\"", json, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("flagValue", json, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("writeup", json, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("Draft lesson", json, StringComparison.Ordinal);
    Assert.Contains("Published lesson", json, StringComparison.Ordinal);
}
```

- [x] **Step 2: Run red test**

Run the integration command with `--filter FullyQualifiedName~SkillTreeQueryTests`.

Expected: `404` because `/api/skill-trees` does not exist.

- [x] **Step 3: Define response records**

Add these public records to `SkillTreeContracts.cs`:

```csharp
public sealed record SkillTreeSummaryResponse(
    Guid SkillTreeId, string Name, string Summary, string IconKey,
    int CategoryCount, int ChallengeCount, int LessonCount);

public sealed record SkillTreeDetailResponse(
    Guid SkillTreeId, string Name, string Summary, string IconKey,
    IReadOnlyList<SkillCategoryPublicResponse> Categories);

public sealed record SkillCategoryPublicResponse(
    Guid CategoryId, string Name, string Summary, string IconKey, int SortOrder,
    IReadOnlyList<SkillTreeContentSummaryResponse> Contents);

public sealed record SkillTreeContentSummaryResponse(
    Guid ContentId, string Kind, int SortOrder, string Title, string Summary,
    int ExpectedMinutes, string Difficulty);
```

`Kind` 只能是 `challenge` 或 `lesson`。课节 `Difficulty` 返回空字符串。

- [x] **Step 4: Implement batched query service**

`ListPublishedAsync` 先投影树和当前修订的计数；`GetPublishedAsync` 分两次查询：第一次取树和有序类别引用，第二次用 `categoryIds.Contains` 批量取已发布内容。禁止在类别循环中访问数据库。

核心过滤条件必须逐字体现：

```csharp
tree.DeletedAtUtc == null &&
tree.CurrentPublishedRevisionId != null &&
tree.CurrentPublishedRevision != null
```

内容过滤：

```csharp
content.Category.DeletedAtUtc == null &&
((content.ChallengeId != null &&
  content.Challenge!.PublicationState == ChallengePublicationState.Published &&
  content.Challenge.IsEnabled) ||
 (content.LessonId != null &&
  content.Lesson!.PublicationState == LessonPublicationState.Published))
```

- [x] **Step 5: Add anonymous controller and register service**

```csharp
[ApiController]
[Route("api/skill-trees")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class SkillTreesController(SkillTreeQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillTreeSummaryResponse>>> List(CancellationToken token) =>
        Ok(await service.ListPublishedAsync(token));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SkillTreeDetailResponse>> Detail(Guid id, CancellationToken token) =>
        await service.GetPublishedAsync(id, token) is { } result ? Ok(result) : NotFound();
}
```

- [x] **Step 6: Run focused tests and commit**

Expected: all `SkillTreeQueryTests` pass, and the command log contains a bounded number of SQL commands independent of category count.

```bash
git add src/GZCTF/Features/SkillTrees/Application \
  src/GZCTF/Features/SkillTrees/Api/SkillTreesController.cs \
  src/GZCTF/Extensions/Startup/ServicesExtension.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeQueryTests.cs
git commit -m "feat: expose public skill tree queries"
```

## Task 2: ST07 — Admin tree draft and atomic publication

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/AdminSkillTreeService.cs`
- Create: `src/GZCTF/Features/SkillTrees/Application/SkillTreeExceptions.cs`
- Create: `src/GZCTF/Features/SkillTrees/Api/AdminSkillTreesController.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreePublishingTests.cs`

- [x] **Step 1: Add failing publication tests**

```csharp
[Fact]
public async Task Empty_tree_can_be_published()
{
    var created = await CreateTreeAsync("Empty", "flag");
    var result = await admin.PostAsJsonAsync(
        $"/api/admin/skill-trees/{created.SkillTreeId}/publish",
        new PublishSkillTreeCommand(created.RowVersion));
    Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
    Assert.Empty((await user.GetFromJsonAsync<SkillTreeDetailResponse>(
        $"/api/skill-trees/{created.SkillTreeId}"))!.Categories);
}

[Fact]
public async Task Stale_row_version_does_not_switch_published_revision()
{
    var created = await CreateTreeAsync("Concurrent", "web");
    await SaveDraftAsync(created.SkillTreeId, created.RowVersion, firstCategoryId);
    var stale = await admin.PostAsJsonAsync(
        $"/api/admin/skill-trees/{created.SkillTreeId}/publish",
        new PublishSkillTreeCommand(created.RowVersion));
    Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    Assert.Equal("skill_tree_revision_conflict", await ReadCodeAsync(stale));
}
```

Also assert duplicate category IDs and noncontinuous sort order return `400`, and readers see either the old complete revision or the new complete revision during publish.

- [x] **Step 2: Run red test**

Expected: `404` for the admin route.

- [x] **Step 3: Define commands and admin responses**

```csharp
public sealed record CreateSkillTreeCommand(string Name, string Summary, string IconKey);
public sealed record UpdateSkillTreeDraftCommand(
    string Name, string Summary, string IconKey, uint RowVersion,
    IReadOnlyList<SkillTreeCategoryOrderCommand> Categories);
public sealed record SkillTreeCategoryOrderCommand(Guid CategoryId, int SortOrder);
public sealed record PublishSkillTreeCommand(uint RowVersion);
public sealed record AdminSkillTreeResponse(
    Guid SkillTreeId, string Name, string Summary, string IconKey,
    bool IsPublished, bool HasDraft, uint RowVersion);
public sealed record SkillTreeDraftResponse(
    Guid SkillTreeId, Guid RevisionId, string Name, string Summary, string IconKey,
    uint RowVersion, IReadOnlyList<SkillTreeCategoryAdminResponse> Categories);
```

- [x] **Step 4: Implement draft lifecycle**

`CreateAsync` validates trimmed name and icon, then creates a tree plus one empty draft. `GetDraftAsync` copies only category references from the current published revision when no draft exists. `UpdateDraftAsync` replaces draft refs after verifying all category IDs are active and unique, then normalizes sort order to `0..n-1`.

`PublishAsync` must run in a serializable transaction, execute `SELECT "Id" ... FOR UPDATE`, verify revision `xmin`, archive the old published revision, mark the draft published, switch `CurrentPublishedRevisionId`, commit, and leave no draft. Empty `Categories` is valid.

- [x] **Step 5: Map stable problems in controller**

Every endpoint catches only known domain exceptions. Map validation to `400`, not found to `404`, row version conflict to `409`. Add `code` through `ProblemDetails.Extensions["code"]`.

- [x] **Step 6: Run tests and commit**

Run `SkillTreePublishingTests` twice. Expected: all pass and no orphan draft is created.

```bash
git add src/GZCTF/Features/SkillTrees/Application/AdminSkillTreeService.cs \
  src/GZCTF/Features/SkillTrees/Application/SkillTreeExceptions.cs \
  src/GZCTF/Features/SkillTrees/Api/AdminSkillTreesController.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreePublishingTests.cs
git commit -m "feat: publish skill tree revisions"
```

## Task 3: ST08 — Shared category CRUD and global ordering

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/SkillCategoryService.cs`
- Create: `src/GZCTF/Features/SkillTrees/Api/AdminSkillCategoriesController.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillCategoryTests.cs`

- [x] **Step 1: Add failing shared-content tests**

```csharp
[Fact]
public async Task Reordering_one_category_changes_every_referencing_tree()
{
    await PublishTwoTreesReferencingAsync(categoryId);
    await admin.PutAsJsonAsync($"/api/admin/skill-categories/{categoryId}/contents",
        new UpdateCategoryContentsCommand(categoryRowVersion,
        [new("lesson", lessonId, 0), new("challenge", challengeId, 1)]));

    var first = await user.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{firstTreeId}");
    var second = await user.GetFromJsonAsync<SkillTreeDetailResponse>($"/api/skill-trees/{secondTreeId}");
    Assert.Equal(new[] { lessonId, challengeId }, first!.Categories.Single().Contents.Select(x => x.ContentId));
    Assert.Equal(new[] { lessonId, challengeId }, second!.Categories.Single().Contents.Select(x => x.ContentId));
}
```

Also cover orphan category creation, editing metadata, tree membership reporting, duplicate content rejection, invalid kind, deleted content, and continuous order normalization.

- [x] **Step 2: Define category contracts**

```csharp
public sealed record SkillCategoryCommand(string Name, string Summary, string IconKey, uint? RowVersion);
public sealed record CategoryContentOrderCommand(string Kind, Guid ContentId, int SortOrder);
public sealed record UpdateCategoryContentsCommand(
    uint RowVersion, IReadOnlyList<CategoryContentOrderCommand> Contents);
public sealed record CategoryTreeMembershipCommand(
    Guid SkillTreeId, bool Included, uint SkillTreeRowVersion);
public sealed record UpdateCategoryTreeMembershipsCommand(
    uint CategoryRowVersion, IReadOnlyList<CategoryTreeMembershipCommand> Trees);
public sealed record SkillCategoryAdminResponse(
    Guid CategoryId, string Name, string Summary, string IconKey, uint RowVersion,
    IReadOnlyList<CategoryTreeReferenceResponse> Trees,
    IReadOnlyList<SkillTreeContentSummaryResponse> Contents);
public sealed record CategoryTreeReferenceResponse(Guid SkillTreeId, string Name, bool IsPublished);
```

- [x] **Step 3: Implement category service**

Create and update validate the fixed icon catalog. Listing returns active categories including orphans. Tree membership includes active draft and current published references but de-duplicates by tree ID. Content replacement validates exactly one of challenge/lesson per command, checks publication state, removes active rows, inserts normalized rows, and saves once inside a transaction.

`UpdateTreeMembershipsAsync` locks affected trees in ID order, verifies every tree row version, creates a draft from the current published revision when needed, adds the category at the end or removes it from each draft, and normalizes draft order. It never mutates a published revision and never publishes automatically. Return affected draft IDs so the administrator can preview and publish each tree explicitly.

- [x] **Step 4: Implement controller and cache invalidation seam**

Introduce `ISkillTreeCacheInvalidator.InvalidateByCategoryAsync(Guid, CancellationToken)` with a no-op implementation until a distributed cache is added. Call it only after the transaction commits. Tests inject a spy and assert one invalidation per successful mutation and zero on rollback.

- [x] **Step 5: Run tests and commit**

```bash
git add src/GZCTF/Features/SkillTrees/Application/SkillCategoryService.cs \
  src/GZCTF/Features/SkillTrees/Api/AdminSkillCategoriesController.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillCategoryTests.cs
git commit -m "feat: manage shared skill categories"
```

## Task 4: ST09 — Category impact, soft delete, and merge

**Files:**
- Modify: `src/GZCTF/Features/SkillTrees/Application/SkillCategoryService.cs`
- Modify: `src/GZCTF/Features/SkillTrees/Api/AdminSkillCategoriesController.cs`
- Modify: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillCategoryTests.cs`

- [x] **Step 1: Add failing delete and merge tests**

```csharp
[Fact]
public async Task Delete_preserves_published_audit_refs_and_progress()
{
    var beforeProgress = await CountProgressAsync();
    var response = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
        $"/api/admin/skill-categories/{categoryId}")
    {
        Content = JsonContent.Create(new DeleteCategoryCommand(categoryName, categoryRowVersion))
    });
    response.EnsureSuccessStatusCode();
    Assert.Equal(beforeProgress, await CountProgressAsync());
    Assert.True(await PublishedAuditReferenceExistsAsync(categoryId));
    Assert.False(await DraftReferenceExistsAsync(categoryId));
}

[Fact]
public async Task Merge_deduplicates_tree_and_content_refs_and_keeps_survivor_order_first()
{
    await MergeAsync(survivorId, duplicateId, survivorRowVersion, duplicateRowVersion);
    Assert.Equal(new[] { survivorFirstId, sharedId, duplicateOnlyId },
        await ReadCategoryContentIdsAsync(survivorId));
    Assert.Equal(survivorId, await ReadMergedIntoAsync(duplicateId));
}
```

Also assert a stale version rolls back every change, self-merge fails, deleted categories cannot be merged, and duplicate tree refs are reduced to one per revision with continuous order.

- [x] **Step 2: Define impact and mutation contracts**

```csharp
public sealed record CategoryDeleteImpactResponse(
    Guid CategoryId, string Name, int DraftTreeCount, int PublishedTreeCount,
    int ChallengeCount, int LessonCount, bool RequiresTypedConfirmation);
public sealed record DeleteCategoryCommand(string ConfirmationName, uint RowVersion);
public sealed record MergeSkillCategoryCommand(
    Guid SurvivorCategoryId, Guid DuplicateCategoryId,
    uint SurvivorRowVersion, uint DuplicateRowVersion);
```

- [x] **Step 3: Implement delete transaction**

Lock category row, re-read impact, verify full trimmed name and version, set `DeletedAtUtc`, remove refs only from draft revisions, retain published refs and all `CategoryContent` rows, save, commit, invalidate referencing trees. Public queries already filter deleted categories.

- [x] **Step 4: Implement merge transaction**

Lock category IDs in sorted order to prevent deadlock. For every revision referencing the duplicate, keep an existing survivor ref or retarget the duplicate ref, then normalize revision order. Append duplicate-only content after survivor content in duplicate sort order; remove duplicate content rows already present on survivor. Set duplicate `DeletedAtUtc` and `MergedIntoCategoryId`. Catch unique/concurrency violations, roll back, return `409 skill_category_merge_conflict`.

- [x] **Step 5: Run tests and commit**

```bash
git add src/GZCTF/Features/SkillTrees/Application/SkillCategoryService.cs \
  src/GZCTF/Features/SkillTrees/Api/AdminSkillCategoriesController.cs \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillCategoryTests.cs
git commit -m "feat: merge and soft delete skill categories"
```

## Task 5: ST10–ST11 — Challenge and lesson publication

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/ContentPublicationService.cs`
- Modify: `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs`
- Modify: `src/GZCTF/Features/ChallengeLibrary/Api/AdminChallengesController.cs`
- Modify: `src/GZCTF/Features/ChallengeLibrary/Api/AdminLessonsController.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/ContentPublicationTests.cs`

- [x] **Step 1: Add failing publication matrix**

Use one theory for challenge and lesson:

```csharp
[Theory]
[InlineData("challenge")]
[InlineData("lesson")]
public async Task Draft_can_be_uncategorized_but_publish_requires_active_tree_category(string kind)
{
    var contentId = await SaveUncategorizedDraftAsync(kind);
    var noCategory = await PublishAsync(kind, contentId, [], []);
    Assert.Equal(HttpStatusCode.BadRequest, noCategory.StatusCode);
    Assert.Equal("content_category_required", await ReadCodeAsync(noCategory));

    var orphan = await CreateOrphanCategoryAsync();
    var noTree = await PublishAsync(kind, contentId, [orphan.CategoryId], []);
    Assert.Equal(HttpStatusCode.BadRequest, noTree.StatusCode);
    Assert.Equal("content_category_has_no_active_tree", await ReadCodeAsync(noTree));
}
```

Add cases for multiple categories, duplicate IDs, inactive category, re-publication changing categories, default append order, and one transaction rollback.

- [x] **Step 2: Define publication command**

```csharp
public sealed record InlineSkillCategoryCommand(
    Guid SkillTreeId, string Name, string Summary, string IconKey);
public sealed record PublishContentCommand(
    uint RowVersion,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<InlineSkillCategoryCommand> InlineCategories);
```

Extend administrator edit responses so both editors can reopen the publication state without another custom request:

```csharp
public sealed record ChallengePublicationEditState(
    uint RowVersion, ChallengePublicationState PublicationState,
    IReadOnlyList<Guid> CategoryIds);
public sealed record LessonPublicationEditState(
    uint RowVersion, LessonPublicationState PublicationState,
    IReadOnlyList<Guid> CategoryIds);
```

Add these values to `ChallengeEditResponse` and `LessonResponse`. Never include category IDs in anonymous challenge or lesson responses.

Each inline category targets exactly one active skill tree. A tree with an empty current published revision gets a new draft copied from current, the category is appended, and that draft is published in the same transaction as content publication.

- [x] **Step 3: Implement the common transaction**

`PublishChallengeAsync` and `PublishLessonAsync` call one private generic transaction body. It validates content row version and publishable localized content, creates inline categories, attaches them to the selected trees, validates every final category belongs to at least one active tree draft or current revision, replaces category links, appends with `max(SortOrder) + 1`, changes state to `Published`, and commits once.

If the selected tree already has categories, inline creation remains allowed and appends the new category. If a tree has no draft, create one from current before adding; if it has no published revision, use its existing draft and publish it. Any error rolls back tree, category, links, and publication state together.

- [x] **Step 4: Add controller endpoints**

```csharp
[HttpPost("{id:guid}/publish")]
public async Task<IActionResult> Publish(Guid id, PublishContentCommand command, CancellationToken token)
{
    await publicationService.PublishChallengeAsync(id, command, token);
    return NoContent();
}
```

Use the equivalent method for lessons. Keep existing save endpoints as draft saves; changing `PublicationState` directly through `ChallengeCommand` or `LessonCommand` must be ignored or rejected so publication cannot bypass category validation.

- [x] **Step 5: Run focused and runtime regression tests**

Run `ContentPublicationTests`, `ChallengeLibraryTests`, `ChallengeModeContractTests`, and `ChallengeHelpTests`. Expected: all pass; the four runtime modes, hints, and WP remain unchanged.

- [x] **Step 6: Commit**

```bash
git add src/GZCTF/Features/SkillTrees/Application/ContentPublicationService.cs \
  src/GZCTF/Features/ChallengeLibrary \
  src/GZCTF.Integration.Test/Tests/SkillTrees/ContentPublicationTests.cs
git commit -m "feat: publish categorized challenges and lessons"
```

## Task 6: ST12 — Enrollment, personal record, and tree deletion

**Files:**
- Create: `src/GZCTF/Features/SkillTrees/Application/SkillTreeEnrollmentService.cs`
- Create: `src/GZCTF/Features/SkillTrees/Api/SkillTreeEnrollmentsController.cs`
- Create: `src/GZCTF/Features/SkillTrees/Api/MyLearningController.cs`
- Modify: `src/GZCTF/Features/SkillTrees/Application/AdminSkillTreeService.cs`
- Modify: `src/GZCTF/Features/SkillTrees/Api/AdminSkillTreesController.cs`
- Create: `src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeEnrollmentTests.cs`

- [x] **Step 1: Add failing enrollment and progress tests**

```csharp
[Fact]
public async Task Shared_solve_counts_in_every_tree_without_duplicate_progress_rows()
{
    await EnrollAsync(userId, firstTreeId);
    await EnrollAsync(userId, secondTreeId);
    await SolveAsync(userId, sharedChallengeId);

    var record = await client.GetFromJsonAsync<MyLearningResponse>("/api/my-learning");
    Assert.All(record!.SkillTrees, tree => Assert.Equal(1, tree.CompletedChallengeCount));
    Assert.Equal(1, await CountChallengeProgressAsync(userId, sharedChallengeId));
}

[Fact]
public async Task Deleting_current_tree_clears_pointer_but_keeps_enrollment_and_history()
{
    await DeleteTreeAsync(treeId, treeName, rowVersion);
    Assert.False(await HasCurrentEnrollmentAsync(userId));
    Assert.True(await EnrollmentExistsAsync(userId, treeId));
    Assert.Contains((await GetRecordAsync()).SkillTrees, x => x.SkillTreeId == treeId && x.IsDeleted);
}
```

Also cover first enrollment becoming current, selecting another tree, leaving current tree, rejecting deleted/unpublished tree enrollment, and category progress computed with distinct content IDs.

- [x] **Step 2: Define contracts**

```csharp
public sealed record SkillTreeEnrollmentResponse(
    Guid EnrollmentId, Guid SkillTreeId, string Name, string IconKey,
    bool IsCurrent, DateTimeOffset EnrolledAtUtc);
public sealed record MyLearningResponse(
    Guid? CurrentSkillTreeId, IReadOnlyList<MySkillTreeRecordResponse> SkillTrees,
    IReadOnlyList<RecentLearningActivityResponse> RecentActivity);
public sealed record MySkillTreeRecordResponse(
    Guid SkillTreeId, string Name, string IconKey, bool IsCurrent, bool IsDeleted,
    int CategoryCount, int CompletedCategoryCount,
    int ChallengeCount, int CompletedChallengeCount,
    int LessonCount, int CompletedLessonCount);
public sealed record SkillTreeDeleteImpactResponse(
    Guid SkillTreeId, string Name, int CategoryCount, int ChallengeCount,
    int LessonCount, int EnrollmentCount, bool IsPublished,
    bool RequiresTypedConfirmation, uint RowVersion);
public sealed record DeleteSkillTreeCommand(string ConfirmationName, uint RowVersion);
```

- [x] **Step 3: Implement enrollment transactions**

Enroll only active published trees. In a serializable transaction, insert once and mark current only when the user has no current active enrollment. Selecting clears every current row for the user before setting the target. Leaving deletes only the enrollment; when leaving current, do not guess a replacement.

- [x] **Step 4: Implement live progress projection**

Load the user's enrollments, current published category refs, active category contents, `ChallengeProgress`, and `LessonProgress` in bounded set queries. For each tree use distinct challenge/lesson IDs. A category is complete only when it has at least one active published content item and every distinct item is complete. Deleted trees use their retained last published revision for history and are marked `IsDeleted`.

- [x] **Step 5: Implement tree impact and soft delete**

Impact counts distinct active categories, challenges, lessons, and enrollments. Delete locks the tree, verifies version and exact trimmed name when impact requires it, sets `DeletedAtUtc`, clears `IsCurrent` on all its enrollments, retains revisions, refs, categories, content and progress, commits, then invalidates the tree cache.

- [x] **Step 6: Run tests and commit**

```bash
git add src/GZCTF/Features/SkillTrees/Application \
  src/GZCTF/Features/SkillTrees/Api \
  src/GZCTF.Integration.Test/Tests/SkillTrees/SkillTreeEnrollmentTests.cs
git commit -m "feat: track skill tree enrollment and learning records"
```

## Task 7: ST13 — OpenAPI contract and generated TypeScript client

**Files:**
- Modify: `src/GZCTF/Features/SkillTrees/Api/*.cs`
- Modify generated: `src/GZCTF/ClientApp/src/Api.ts`

- [x] **Step 1: Verify OpenAPI tags before generation**

Controllers must use stable names or explicit tags: `SkillTrees`, `AdminSkillTrees`, `AdminSkillCategories`, `SkillTreeEnrollments`, `MyLearning`, `AdminChallenges`, and `AdminLessons`. Start the app and assert:

```bash
curl -fsS http://localhost:8080/openapi/v1.json > /tmp/gzctf-openapi.json
jq -e '.paths["/api/skill-trees"]' /tmp/gzctf-openapi.json
jq -e '.paths["/api/admin/skill-categories/merge"]' /tmp/gzctf-openapi.json
jq -e '.paths["/api/my-learning"]' /tmp/gzctf-openapi.json
```

Expected: every command and response schema is present and no administrator write route is anonymous.

- [x] **Step 2: Regenerate the client**

```bash
cd src/GZCTF/ClientApp
pnpm genapi
```

Do not edit `Api.ts` by hand. Search for generated modules and stable fields:

```bash
rg -n 'SkillTrees|AdminSkillTrees|AdminSkillCategories|MyLearning|content_category_required' src/Api.ts
```

- [x] **Step 3: Compile both sides**

Run backend build, focused ST2 integration tests, `pnpm check`, and `pnpm build`. Expected: zero errors.

- [x] **Step 4: Commit generated contract**

```bash
git add src/GZCTF/Features/SkillTrees/Api src/GZCTF/ClientApp/src/Api.ts
git commit -m "chore: generate skill tree api client"
```

## Task 8: ST2 complete backend gate

**Files:**
- Modify only failing test or implementation files identified by this gate.

- [x] **Step 1: Run focused API suite**

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
  dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Debug \
  --filter 'FullyQualifiedName~SkillTree'
```

- [x] **Step 2: Run complete backend suites**

Run the roadmap unit and integration commands. Expected: no failure and existing `/api/learning-paths` tests still pass.

- [x] **Step 3: Inspect security and performance boundaries**

Capture the public list and detail JSON. Assert no `flag`, `tokenHash`, `writeup.content`, unpublished body, email, real name, or student number occurs. With 1 and 20 categories, assert detail SQL command count is constant.

- [x] **Step 4: Check repository diff**

```bash
git diff --check
git status --short
```

Expected: no whitespace error; only intended ST2 changes remain.

## Wave ST2 completion checklist

- [x] 空技能树可发布和匿名查看。
- [x] 技能树结构发布原子切换，过期行版本返回稳定 `409`。
- [x] 同一类别的内容与顺序在所有技能树实时一致。
- [x] 未分类草稿允许保存，未分类或孤儿类别内容不能发布。
- [x] 空技能树可在内容发布事务内创建类别并完成发布。
- [x] 类别删除和合并保留题目、课节、完成记录及发布审计引用。
- [x] 技能树删除清除当前指针并保留历史。
- [x] 共享题目只存一条完成记录，并在所有技能树进度中计入。
- [x] OpenAPI 和 `Api.ts` 由运行中的服务生成且能编译。
- [x] 旧 Learning API 在本阶段仍可用。
