# 比赛选择题

在「比赛后台 → 选择题」启用功能，设置单选、多选的题目数量和每题分值，添加或批量导入题库，然后点击「保存配置」。可与原有 CTF 题目同时使用；仅使用选择题时无需创建 Flag 题目。

- 每队共享一份答卷。个人考试可在比赛信息中将队伍人数上限设为 1。
- 首次点击「开始答题」时按题型随机抽取指定数量，题目、顺序和分值固定保存。各题型数量可设为 0–1000，启用时总题数必须大于 0；题库最多 5000 题。
- 任一队伍开始答题后，题库、题数、分值和启用状态锁定，防止中途更改试卷。
- 选择选项后自动保存至数据库；页面显示保存时间。重新进入会恢复已保存答案，并定位到首道未完成题目。保存失败会保留当前选择并显示重试按钮；离开前请确认保存成功。
- 必须完成并保存所有题目才能交卷。交卷前可修改已保存答案；交卷后全队不可修改，接口同样拒绝修改请求。重复交卷不会重复计分。
- 单选选对得分；多选仅在所有正确选项全部选中且无错选时得分，无部分分。
- 最终成绩计入比赛排行榜总分，遵循分组的默认计分权限。练习模式下赛后仍可作答，但赛后交卷不计入比赛排行榜。关闭练习模式时赛后不能作答或交卷，已保存答卷仍可查看。
- 多窗口或队友同时操作时使用版本检查，旧版本保存会被拒绝。请刷新恢复最新服务器状态后再作答。

## 批量导入

支持 UTF-8 JSON 和 CSV，后台提供模板下载、追加或替换题库、导入预览和 JSON 题库导出。文件上限为 20 MB；整批校验通过后才应用，任一题目不合法会拒绝整次导入。页面预览完成后还需点击「保存配置」。

JSON 接受题目数组，或带有 `questions` 数组的对象。`type` 为 `Single` 或 `Multiple`；`correctAnswers` 使用从 0 开始的选项索引：

```json
[
  {
    "type": "Single",
    "content": "HTTP 默认端口是？",
    "options": ["80", "443", "22", "53"],
    "correctAnswers": [0]
  },
  {
    "type": "Multiple",
    "content": "下列哪些属于非对称加密算法？",
    "options": ["RSA", "AES", "ECC", "DES"],
    "correctAnswers": [0, 2]
  }
]
```

CSV 表头必须为 `type,content,options,answers`。选项用 `|` 分隔；答案使用 A–Z 字母，含逗号的字段需要双引号。题型也支持「单选」「多选」。选项含有 `|` 时请使用 JSON 格式。

```csv
type,content,options,answers
Single,HTTP 默认端口是？,80|443|22|53,A
Multiple,下列哪些属于非对称加密算法？,RSA|AES|ECC|DES,"A,C"
```

每道题支持 2–26 个非空且不同的选项。单选需一个正确答案，多选需至少两个；正确答案索引不能重复或越界。

## 数据库与验证

新增 `AddChoiceExams` EF Core 迁移，创建 `ChoiceExams` 和 `ChoiceAttempts`，原有题目保持兼容。应用启动时沿用项目已有的自动迁移流程；发布时需要数据库账号具备执行迁移的权限。答卷存在数据库中，需随数据库一起备份。原有整场比赛 ZIP 导入导出尚不包含此独立题库，请使用选择题页面的 JSON 导入导出迁移题库，并重新配置题数和分值。

前端检查（在 `src/GZCTF/ClientApp` 执行）：

```powershell
pnpm check
pnpm build
node --test tests/choice-import.test.mjs
```

后端检查（在仓库根目录执行，许可证路径按实际环境设置）：

```powershell
dotnet test src/GZCTF.Test/GZCTF.Test.csproj -c Release -p:SixLaborsLicenseFile=<许可证文件路径>
dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -c Release --filter FullyQualifiedName~ChoiceExamTests -p:SixLaborsLicenseFile=<许可证文件路径>
```

集成测试使用 Docker 中的临时 PostgreSQL，覆盖迁移、权限隔离、草稿恢复、配置锁定、并发开始、保存与交卷竞争、赛后写入限制和排行榜计分。
