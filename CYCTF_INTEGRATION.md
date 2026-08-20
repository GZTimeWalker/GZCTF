# CYCTF 扩展集成文档

## 概述

本文档描述了集成到 GZCTF 的 CYCTF 扩展功能。这些扩展为 GZCTF 平台添加了高级比赛管理、报名系统和奖项管理功能。

## 功能模块

### 1. 比赛扩展 (GameExtension)
提供比赛报名配置和管理功能。

**数据模型特性：**
- 报名时间段配置（开始/结束）
- 队伍数量限制
- 当前报名队伍统计
- 邮箱白名单控制
- 显示设置（报名人数、活动时间）

**API 端点：**
- `GET /api/cyctf/games/{gameId}/extension` - 获取比赛扩展信息
- `PUT /api/cyctf/games/{gameId}/extension` - 创建或更新扩展信息

### 2. 组别扩展 (DivisionExtension)
为不同组别添加特定配置。

**数据模型特性：**
- 队伍规模限制（最小/最大人数）
- 自定义报名字段（JSON 格式）

**API 端点：**
- `GET /api/cyctf/divisions/{divisionId}/extension` - 获取组别扩展信息
- `PUT /api/cyctf/divisions/{divisionId}/extension` - 创建或更新扩展信息

### 3. 报名系统 (Registration)
完整的队伍报名和审核工作流。

**数据模型特性：**
- 报名状态管理（PENDING, APPROVED, REJECTED, CANCELLED）
- 自定义表单数据存储
- 审核记录（审核人、时间、备注）
- 软删除支持

**API 端点：**
- `POST /api/cyctf/registrations` - 队伍报名
- `GET /api/cyctf/registrations/games/{gameId}` - 获取比赛所有报名（管理员）
- `GET /api/cyctf/registrations/games/{gameId}/teams/{teamId}` - 获取队伍报名记录
- `POST /api/cyctf/registrations/{id}/review` - 审核报名（管理员）
- `GET /api/cyctf/registrations/games/{gameId}/stats` - 获取报名统计（管理员）

### 4. 赞助商管理 (Sponsor)
管理比赛赞助商信息。

**数据模型特性：**
- 赞助商基本信息（名称、描述、Logo）
- 外部链接
- 显示优先级排序

**API 端点：**
- `GET /api/cyctf/sponsors/games/{gameId}` - 获取比赛赞助商列表
- `POST /api/cyctf/sponsors` - 创建赞助商（管理员）
- `PUT /api/cyctf/sponsors/{id}` - 更新赞助商（管理员）
- `DELETE /api/cyctf/sponsors/{id}` - 删除赞助商（管理员）

### 5. 奖项管理 (Award)
管理比赛奖项和获奖队伍。

**数据模型特性：**
- 奖项信息（名称、描述、图标）
- 获奖队伍记录
- 显示优先级排序

**API 端点：**
- `GET /api/cyctf/awards/games/{gameId}` - 获取比赛奖项列表
- `POST /api/cyctf/awards` - 创建奖项（管理员）
- `PUT /api/cyctf/awards/{id}` - 更新奖项（管理员）
- `DELETE /api/cyctf/awards/{id}` - 删除奖项（管理员）

## 技术实现

### 数据库架构

新增的数据表：
1. **GameExtensions** - 比赛扩展配置
2. **DivisionExtensions** - 组别扩展配置
3. **Registrations** - 报名记录
4. **Sponsors** - 赞助商信息
5. **Awards** - 奖项信息

所有表均包含：
- 软删除标记 (`Deleted`)
- 时间戳 (`CreateTime`, `UpdateTime`)
- 适当的外键约束和级联删除策略

### 数据库迁移

迁移文件：`20260819223947_AddCyctfExtensions.cs`

应用迁移：
```bash
cd src/GZCTF
dotnet ef database update
```

### 依赖注入配置

在 `ServicesExtension.cs` 中注册的服务：
```csharp
services.AddScoped<IGameExtensionRepository, GameExtensionRepository>();
services.AddScoped<IDivisionExtensionRepository, DivisionExtensionRepository>();
services.AddScoped<IRegistrationRepository, RegistrationRepository>();
services.AddScoped<ISponsorRepository, SponsorRepository>();
services.AddScoped<IAwardRepository, AwardRepository>();
```

### 权限控制

- 公开 API：报名接口、查询自己的报名记录
- 管理员 API：审核报名、管理赞助商、管理奖项、查看所有报名记录

## 代码结构

```
src/GZCTF/
├── Controllers/Cyctf/
│   ├── GameExtensionController.cs
│   ├── DivisionExtensionController.cs
│   ├── RegistrationController.cs
│   ├── SponsorController.cs
│   └── AwardController.cs
├── Models/
│   ├── Data/Cyctf/
│   │   ├── GameExtension.cs
│   │   ├── DivisionExtension.cs
│   │   ├── Registration.cs
│   │   ├── Sponsor.cs
│   │   └── Award.cs
│   ├── Request/Cyctf/
│   │   ├── GameExtensionRequest.cs
│   │   ├── DivisionExtensionRequest.cs
│   │   ├── RegistrationRequest.cs
│   │   ├── RegistrationReviewRequest.cs
│   │   ├── SponsorRequest.cs
│   │   └── AwardRequest.cs
│   └── Response/
│       └── Cyctf/
│           ├── GameExtensionResponse.cs
│           ├── DivisionExtensionResponse.cs
│           ├── RegistrationResponse.cs
│           ├── SponsorResponse.cs
│           └── AwardResponse.cs
└── Repositories/
    ├── Interface/
    │   ├── IGameExtensionRepository.cs
    │   ├── IDivisionExtensionRepository.cs
    │   ├── IRegistrationRepository.cs
    │   ├── ISponsorRepository.cs
    │   └── IAwardRepository.cs
    ├── GameExtensionRepository.cs
    ├── DivisionExtensionRepository.cs
    ├── RegistrationRepository.cs
    ├── SponsorRepository.cs
    └── AwardRepository.cs
```

## 使用流程

### 1. 配置比赛扩展
1. 创建比赛后，通过 `PUT /api/cyctf/games/{gameId}/extension` 配置报名参数
2. 设置报名时间段、队伍数量限制等

### 2. 配置组别扩展
1. 为每个组别通过 `PUT /api/cyctf/divisions/{divisionId}/extension` 配置规则
2. 设置队伍规模限制、自定义报名字段

### 3. 队伍报名
1. 队伍通过 `POST /api/cyctf/registrations` 提交报名
2. 系统验证报名时间、队伍数量限制等
3. 报名状态初始为 PENDING

### 4. 审核报名
1. 管理员通过 `GET /api/cyctf/registrations/games/{gameId}` 查看所有报名
2. 通过 `POST /api/cyctf/registrations/{id}/review` 审核报名
3. 设置状态为 APPROVED 或 REJECTED

### 5. 管理赞助商和奖项
1. 通过对应的管理接口添加赞助商和奖项信息
2. 前端展示给参赛者

## 注意事项

1. **SixLabors.ImageSharp 版本问题**
   - 当前降级到 3.1.6 版本以避免许可证验证问题
   - 生产环境可能需要获取正式许可证或使用其他图像处理库

2. **.NET SDK 版本**
   - `global.json` 已更新为 10.0.103
   - 确保开发环境安装了相应版本

3. **数据库兼容性**
   - 使用 PostgreSQL 作为数据库
   - 迁移使用了 Npgsql Entity Framework Core Provider

4. **API 文档**
   - 所有 API 均包含 XML 文档注释
   - 可通过 Swagger/OpenAPI 查看完整文档

## 后续开发建议

1. **前端集成**
   - 开发报名表单界面
   - 实现管理员审核界面
   - 显示赞助商和奖项信息

2. **功能增强**
   - 添加邮件通知（报名确认、审核结果）
   - 报名数据导出功能
   - 批量审核功能
   - 报名统计可视化

3. **安全加固**
   - 实现频率限制防止刷报名
   - 添加验证码防止自动化注册
   - 敏感数据加密存储

4. **性能优化**
   - 添加缓存层
   - 数据库查询优化
   - 大量报名时的分页加载

## 测试

构建项目验证：
```bash
cd src/GZCTF
dotnet build
```

应该看到 0 个错误的输出。

## 版本信息

- **集成日期**: 2026-08-19
- **GZCTF 版本**: 1.8.7
- **.NET 版本**: 10.0.103
- **数据库迁移**: 20260819223947_AddCyctfExtensions

## 相关文件

- 数据库迁移: `src/GZCTF/Migrations/20260819223947_AddCyctfExtensions.cs`
- DbContext 更新: `src/GZCTF/Models/AppDbContext.cs`
- 服务注册: `src/GZCTF/Extensions/Startup/ServicesExtension.cs`
- 包依赖: `src/Directory.Packages.props`
