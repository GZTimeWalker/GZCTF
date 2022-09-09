<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/banner.dark.svg">
  <img alt="Banner" src="assets/banner.light.svg">
</picture>

# GZ::CTF

[![publish](https://github.com/GZTimeWalker/GZCTF/actions/workflows/ci.yml/badge.svg)](https://github.com/GZTimeWalker/GZCTF/actions/workflows/ci.yml)
![version](https://img.shields.io/github/v/release/GZTimeWalker/GZCTF?include_prereleases&label=version)
![license](https://img.shields.io/github/license/GZTimeWalker/GZCTF?color=FF5531)
[![Telegram Group](https://img.shields.io/endpoint?color=blue&url=https%3A%2F%2Ftg.sumanjay.workers.dev%2Fgzctf)](https://telegram.dog/gzctf)

GZ::CTF 是一个基于 ASP.NET Core 的开源 CTF 平台。

## 特性

- 创建高度可自定义的题目
  - 题目类型：静态附件、动态附件、静态容器、动态容器
    - 静态附件：共用附件，任意添加的 flag 均可提交。
    - 动态附件：需要至少满足队伍数量的 flag 和附件，附件及 flag 按照队伍进行分发。
    - 静态容器：共用容器，任意添加的 flag 均可提交。
    - 动态容器：自动生成并通过容器环境变量进行 flag 下发，每个队伍 flag 唯一。
  - 动态分值
    - 分值曲线：
        $$f(S, r, d, x) = \left \lfloor S \times \left[r  + ( 1- r) \times exp\left( \dfrac{1 - x}{d} \right) \right] \right \rfloor $$
      其中 $S$ 为原始分值、 $r$ 为最低分值比例、 $d$ 为难度系数、 $x$ 为提交次数。前三个参数可通过自定义实现绝大部分的动态分值需求。
    - 三血奖励：
      平台对一二三血分别奖励 5%、3%、1% 的当前题目分值
  - 比赛进行中可启用新题
  - 动态 flag 中启用作弊检测，可选的 flag 模版
- 基于 Docker 或 K8s 的动态容器分发
- 动态展示可缩放的前十名队伍得分时间线、动态隐藏的积分榜
- 基于 signalR 的实时比赛通知、比赛事件和 flag 提交监控及日志监控
- SMTP 注册邮件发送、基于 Google ReCaptchav3 的恶意注册防护
- 用户封禁、用户三级权限管理
- 可选的队伍审核、邀请码、分组排行
- 比赛期间裁判监控、提交和主要事件日志
- 应用内全局设置
- 以及更多……

## Demo

![](assets/demo-1.png)
![](assets/demo-2.png)
![](assets/demo-3.png)
![](assets/demo-4.png)
![](assets/demo-5.png)
![](assets/demo-6.png)
![](assets/demo-7.png)

## 安装配置

应用已编译打包成 Docker 镜像，可通过以下方式获取：

```bash
docker pull gztime/gzctf:latest
# or
docker pull ghcr.io/gztimewalker/gzctf/gzctf:latest
```

也可使用根目录下的 `docker-compose.yml` 文件进行配置。

题目配置和题目示例请见 [GZCTF-Challenges](https://github.com/GZTimeWalker/GZCTF-Challenges) 仓库。

### `appsettings.json` 配置

为了使注册功能正常使用，请补全 `EmailConfig` 及 `GoogleRecaptcha` 部分，其中验证码请借由 [recaptcha](https://www.google.com/recaptcha/admin) 处注册，并使用 reCAPTCHAv3。

当 `ContainerProvider` 为 `Docker` 时：
  - 如需使用本地 docker，请将 Uri 置空，并将 `/var/run/docker.sock` 挂载入容器对应位置
  - 如需使用外部 docker，请将 Uri 指向对应 docker API Server

当 `ContainerProvider` 为 `K8s` 时：
  - 请将集群连接配置放入 `k8sconfig.yaml` 文件中，并将其挂载到 `/app` 目录下

```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=db:5432;Database=gzctf;Username=postgres;Password=Fyjd0HtrL00QD555W1b6WLKbLl62cHT0"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "EmailConfig": {
    "SendMailAddress": "a@a.com",
    "UserName": "",
    "Password": "",
    "Smtp": {
      "Host": "localhost",
      "Port": 587,
      "EnableSsl": true
    }
  },
  "XorKey": "Q22yg09A91YWm1GsOf9VIMiw",
  "ContainerProvider": "Docker", // or K8s
  "DockerConfig": {
    "SwarmMode": false,
    "Uri": "unix:///var/run/docker.sock",
    "PublicIP": "127.0.0.1"
  },
  "RegistryConfig": {
    "UserName": "",
    "Password": "",
    "ServerAddress": ""
  },
  "GoogleRecaptcha": {
    "VerifyAPIAddress": "https://www.recaptcha.net/recaptcha/api/siteverify",
    "Sitekey": "",
    "Secretkey": "",
    "RecaptchaThreshold": "0.5"
  }
}
```

## 初始管理员

生产环境中默认不存在管理员权限用户，需要手动更改数据库条目。当管理员注册完成并成功登录后，进入所选数据库表格后执行：

```sql
update "AspNetUsers" set "Role"=3;
```

## 贡献者

<a href="https://github.com/GZTimeWalker/GZCTF/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=GZTimeWalker/GZCTF" />
</a>

## 关于 i18n

暂不考虑进行多语言适配。

## Stargazers over time

[![Stargazers over time](https://starchart.cc/GZTimeWalker/GZCTF.svg)](https://starchart.cc/GZTimeWalker/GZCTF)
