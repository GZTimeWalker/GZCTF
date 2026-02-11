# Memory Notes (CTF Platform)

## 产品定位
- 服务 100-200 人规模 CTF 比赛
- 题目部署方式：Docker
- 约束：不改语言、不大改项目结构

## 本轮关键决策
- 以“正确性优先”的方式处理计分板缓存：先失效再重建。
- 避免粗暴防抖导致状态丢失，改为 worker 端请求合并。
- 提交链路使用有界队列形成背压，防止高峰期内存膨胀。
- 提交流水线按比赛维度做公平调度，避免单场流量挤占全局 worker。
- 容器创建采用全局并发闸门，防止 Docker 服务抖动。
- 容器创建前做镜像就绪检查和镜像级拉取锁，降低冷启动抖动。

## 可持续执行清单
- 每次改动后跑：
  - `dotnet build src/GZCTF.slnx -v minimal`
  - `dotnet test src/GZCTF.Test/GZCTF.Test.csproj -v minimal`
  - `dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -v minimal`
- 每次涉及计分板逻辑优先跑 Scoreboard 相关集成测试。
