# 独立题库完整编辑器 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让管理工作台的题目管理具备旧比赛题目编辑器的题目能力，并将题目发布到选定技能树类别供学员启动和验证 Flag。

**Architecture:** 扩展现有 canonical challenge，而不恢复按比赛 ID 存储的旧题目。后端增加 CTF 分类字段、结构化运行配置校验及独立题目访问门禁；前端将简化详情页拆成基本信息、运行环境/附件、Flag、帮助内容和发布几个聚焦区块，沿用现有 Mantine 设计语言。

**Tech Stack:** ASP.NET Core 10、EF Core 10/PostgreSQL、React 19、TypeScript、Mantine 9、SWR、xUnit/Testcontainers、Playwright。

---

## Task 1：迁移并暴露 CTF 分类

**Files:** `src/GZCTF/Features/ChallengeLibrary/Domain/ChallengeModels.cs`、`src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs`、`src/GZCTF/Features/Imports/Application/CanonicalImportService.cs`、`src/GZCTF/Features/LearningPaths/Infrastructure/LearningModelConfiguration.cs`、`src/GZCTF/Migrations/`、`src/GZCTF.Integration.Test/Tests/Learning/ChallengeLibraryTests.cs`、`src/GZCTF/ClientApp/src/Api.ts`。

- [ ] **Step 1:** 写测试：创建 Web 题、更新为 Misc、重新读取列表与编辑响应仍为相同分类；导入 `LegacyMetadataJson` 的 Web 分类得到 Web；未知历史分类回退 Misc。运行相关集成测试，确认新断言先失败。
- [ ] **Step 2:** 为 canonical challenge 添加 `ChallengeCategory CtfCategory`（默认 Misc），在命令与响应中添加同名字段；生成 EF 迁移与模型快照，旧行回填 Misc，导入时解析来源分类。生成 `Api.ts`，保持枚举为现有 `ChallengeCategory`。
- [ ] **Step 3:** 运行 `dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj --filter FullyQualifiedName~ChallengeLibraryTests` 与 `pnpm check`，确认分类往返和 API 类型通过。提交本任务文件。

## Task 2：确保运行配置和 Flag 可真实使用

**Files:** `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs`、`src/GZCTF/Features/SkillTrees/Application/ContentPublicationService.cs`、`src/GZCTF/Features/ChallengeRuntime/Application/ChallengeSubmissionService.cs`、`src/GZCTF/Features/ChallengeRuntime/Application/ChallengeRuntimeService.cs`、`src/GZCTF.Integration.Test/Tests/Runtime/ChallengeModeContractTests.cs`。

- [ ] **Step 1:** 写失败测试：静态附件/容器接受任一配置的静态 Flag；容器配置缺字段、附件无有效存储对象、动态附件缺文件 Flag 对时不能发布；合法四种题型可发布并通过对应方式解题。另验证提交次数上限。
- [ ] **Step 2:** 给发布路径增加与题型匹配的配置校验，返回稳定问题码和字段信息；静态提交改为与任一静态 Flag 做常量时间比较。动态附件保存文件/哈希/Flag 对，动态容器模板与运行配置保持一致。
- [ ] **Step 3:** 运行 `ChallengeModeContractTests`、`ContentPublicationTests`，修复出现的回归后提交。

## Task 3：学员访问权限和启停

**Files:** `src/GZCTF/Features/ChallengeLibrary/Application/ChallengeLibraryService.cs`、`src/GZCTF/Features/ChallengeRuntime/Api/ChallengesController.cs`、`src/GZCTF/Features/ChallengeRuntime/Api/ChallengeInstancesController.cs`、`src/GZCTF/Features/ChallengeRuntime/Api/ChallengeSubmissionsController.cs`（按实际文件名定位）、`src/GZCTF/Features/ChallengeRuntime/Application/ChallengeRuntimeService.cs`、`src/GZCTF.Integration.Test/Tests/Runtime/`。

- [ ] **Step 1:** 写集成测试：草稿、停用、退休题目对普通用户的题面、实例、附件、提示、题解及提交均不可访问；已发布且启用并位于可访问技能树中的题目完整通过，管理员预览仍可用。
- [ ] **Step 2:** 集中实现可见性查询，给入口复用；更新 `IsEnabled` 时失效相关技能树读取缓存，避免列表仍显示旧状态。
- [ ] **Step 3:** 运行 Runtime 与 SkillTrees 集成测试并提交。

## Task 4：创建页与完整编辑器

**Files:** `src/GZCTF/ClientApp/src/components/admin/workspace/ChallengesPanel.tsx`、`src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx`、新增 `src/GZCTF/ClientApp/src/components/admin/library/ChallengeBasicsForm.tsx`、`ChallengeRuntimeForm.tsx`、`ChallengeFlagEditor.tsx`、`ChallengeHelpEditor.tsx`、`ChallengePreview.tsx`、`src/GZCTF/ClientApp/src/locales/{zh-CN,en-US}/learning.json`、`src/GZCTF/ClientApp/tests/e2e/admin-challenge-editor.spec.ts`。

- [ ] **Step 1:** 写浏览器测试：创建表单必须选择 CTF 分类和四种运行方式之一；保存后进入编辑页，编辑题面、难度、启停、提示、题解、Flag 和类型适用的运行字段；重新打开内容不丢失。先观察断言失败。
- [ ] **Step 2:** 创建面板提供标题、CTF 分类、运行方式；详情页提供清晰的分区及固定的保存/发布操作。每种模式只展示适用字段，不暴露原始运行 JSON；服务端错误回显到对应字段。保留题型发布后不可更改的规则。
- [ ] **Step 3:** 用现有 `/api/assets` 上传静态/动态附件；按运行时要求存储 `Attachments` 元数据。Flag 编辑支持多条静态值、动态附件逐文件值、动态容器模板，删除前确认。管理员试运行容器时使用受保护的测试入口并可停止。
- [ ] **Step 4:** 运行 `pnpm check`、`pnpm test:unit`、新 E2E；检查桌面与手机宽度、亮暗主题与键盘焦点，修复实际缺陷后提交。

## Task 5：发布与学员闭环

**Files:** `src/GZCTF/ClientApp/src/components/admin/library/ContentPublishModal.tsx`、`src/GZCTF/ClientApp/src/pages/admin/library/challenges/[id].tsx`、`src/GZCTF/ClientApp/src/components/learning/ChallengeWorkspace.tsx`、`src/GZCTF/ClientApp/tests/e2e/admin-challenge-editor.spec.ts`、`src/GZCTF.Integration.Test/Tests/SkillTrees/ContentPublicationTests.cs`。

- [ ] **Step 1:** 写端到端场景：管理员选择技能树与所属全局类别发布，学员加入该树并从类别进入题目，读取介绍、启动容器或下载附件、提交 Flag；停用后从技能树消失且旧直链不可继续答题。
- [ ] **Step 2:** 发布前保存表单且等待新行版本，发布 Modal 选树选类别时清楚展示归属；选择尚未发布的树时提示管理员先发布技能树。409 只要求重新载入核对，不自动覆盖。
- [ ] **Step 3:** 运行 `pnpm build`、相关 Playwright、.NET 单元和集成测试，检查 `git diff --check`、最终 CodeRabbit 审查；把设计与计划文档强制加入版本控制（仓库忽略 `docs/`），提交并更新已有 PR 或另开 PR。

## 完成判定

管理员可以管理独立题库的 CTF 分类、四种运行方式、题面、提示、题解、附件/容器和 Flag，在发布时选择技能树及类别并启停题目；学员能从技能树完成一次真实解题。四种题型和访问限制都有自动化验证，旧导入题目保持可用。
