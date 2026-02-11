# CTF 100-200 人规模优化记录

## 目标约束
- 不改项目语言
- 不做大规模结构重构
- 优先提升高并发提交与计分板稳定性

## 已完成优化

### 1) 计分板缓存一致性与刷新正确性
- 修复 ScoreBoard cache key 版本不一致问题（`int` 与 `string` 路径统一为 `_v6`）。
- 刷新计分板时先失效旧缓存，再异步重建，避免读取陈旧数据。
- 管理端挑战详情接口改为按游戏获取最新计分板，避免 `TryGet` 命中空缓存导致 `AcceptedCount` 异常。

### 2) 缓存刷新削峰（不丢正确性）
- 在 `CacheMaker` 中增加同 key 请求合并（coalescing）：
  - 对队列中连续同 key/同参数的刷新请求进行合并，只执行最后一次。
  - 目标是降低突发提交下的重复计算开销，同时保证最终状态正确。

### 3) 提交高峰背压控制
- `Submission` / `CacheRequest` 的 channel 从无界改为有界队列：
  - `Submission`: 8192
  - `CacheRequest`: 4096
  - `FullMode=Wait`，高峰时提供可控背压，避免内存无限增长。

### 4) Docker 题目并发调度保护
- 新增 `ContainerPolicy.MaxConcurrentContainerStarts`（默认 6，范围 1-64）。
- `DockerManager.CreateContainerAsync` 增加并发信号量闸门：
  - 在题目容器创建/拉取/启动突发时限制并发，降低 Docker daemon 过载风险。

## 回归验证
- `dotnet build src/GZCTF.slnx -v minimal` 通过
- `dotnet test src/GZCTF.Test/GZCTF.Test.csproj -v minimal` 通过（153/153）
- `dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -v minimal` 通过（167/167）

## 后续建议
1. 根据机器规格调参 `MaxConcurrentContainerStarts`（建议 4-10）
2. 在压测环境对 `Submission` channel 长度与排队时延打点
3. 为 cache coalescing 增加指标（合并率、处理时延）
