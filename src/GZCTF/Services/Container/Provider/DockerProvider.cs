﻿using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public class DockerMetadata
{
    /// <summary>
    /// 公共访问入口
    /// </summary>
    public string PublicEntry { get; set; } = string.Empty;

    /// <summary>
    /// Docker 配置
    /// </summary>
    public DockerConfig Config { get; set; } = new();

    /// <summary>
    /// Docker 鉴权用配置
    /// </summary>
    public AuthConfig? Auth { get; set; }

    /// <summary>
    /// 根据配置获取容器名称
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetName(ContainerConfig config)
        => $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_{(config.Flag ?? Guid.NewGuid().ToString()).StrMD5()[..16]}";
}

public class DockerProvider : IContainerProvider<DockerClient, DockerMetadata>
{
    private readonly DockerClient _dockerClient;
    private readonly DockerMetadata _dockerMeta;

    public DockerMetadata GetMetadata() => _dockerMeta;
    public DockerClient GetProvider() => _dockerClient;

    public DockerProvider(IOptions<ContainerProvider> options, IOptions<RegistryConfig> registry, ILogger<DockerProvider> logger)
    {
        _dockerMeta = new()
        {
            Config = options.Value.DockerConfig ?? new(),
            PublicEntry = options.Value.PublicEntry
        };

        DockerClientConfiguration cfg = string.IsNullOrEmpty(_dockerMeta.Config.Uri) ? new() : new(new Uri(_dockerMeta.Config.Uri));

        // TODO: Docker Auth Required
        _dockerClient = cfg.CreateClient();

        // Auth for registry
        if (!string.IsNullOrWhiteSpace(registry.Value.UserName) && !string.IsNullOrWhiteSpace(registry.Value.Password))
        {
            _dockerMeta.Auth = new AuthConfig()
            {
                Username = registry.Value.UserName,
                Password = registry.Value.Password,
            };
        }

        logger.SystemLog($"Docker 初始化成功 ({(string.IsNullOrEmpty(_dockerMeta.Config.Uri) ? "localhost" : _dockerMeta.Config.Uri)})", TaskStatus.Success, LogLevel.Debug);
    }
}

