// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Services.Container.Provider;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

[ExcludeFromCodeCoverage(Justification = "Docker Swarm container not being used in CI/CD tests")]
public class SwarmManager : IContainerManager
{
    readonly DockerClient _client;
    readonly ILogger<SwarmManager> _logger;
    readonly DockerMetadata _meta;

    public SwarmManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<SwarmManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_SwarmMode)],
            TaskStatus.Success,
            LogLevel.Debug);
    }

    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            await _client.Swarm.RemoveServiceAsync(container.ContainerId, token);
        }
        catch (DockerContainerNotFoundException)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                    container.LogId],
                TaskStatus.Success, LogLevel.Debug);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                        container.LogId],
                    TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                _logger.LogDeletionFailedWithHttpContext(container.ContainerId, e.StatusCode, e.ResponseBody);
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    container.LogId]);
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(GZCTF.Models.Internal.ContainerConfig config,
        CancellationToken token = default)
    {
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();

        if (string.IsNullOrWhiteSpace(imageName))
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_UnresolvedImageName), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        ServiceCreateParameters parameters = GetServiceCreateParameters(config);
        ServiceCreateResponse? serviceRes;
        var retry = 0;

    CreateContainer:
        try
        {
            if (retry++ >= 3)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                        parameters.Service.Name],
                    TaskStatus.Failed,
                    LogLevel.Warning);
                return null;
            }

            serviceRes = await _client.Swarm.CreateServiceAsync(parameters, token);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted),
                        parameters.Service.Name],
                    TaskStatus.Duplicate,
                    LogLevel.Warning);

                await _client.Swarm.RemoveServiceAsync(parameters.Service.Name, token);

                goto CreateContainer;
            }

            _logger.LogCreationFailedWithHttpContext(parameters.Service.Name, e.StatusCode, e.ResponseBody);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    parameters.Service.Name]);
            return null;
        }

        var container = new Models.Data.Container { ContainerId = serviceRes.ID, Image = config.Image };

        retry = 0;
        SwarmService? res;
        while (true)
        {
            if (retry++ >= 3)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerPortNotExposed),
                        container.LogId],
                    TaskStatus.Failed,
                    LogLevel.Warning);
                return null;
            }

            res = await _client.Swarm.InspectServiceAsync(container.ContainerId, token);

            if (res is { Endpoint.Ports.Count: > 0 })
                break;

            await Task.Delay(500, token);
        }

        // TODO: Test is needed
        container.Status = ContainerStatus.Running;
        container.StartedAt = res.CreatedAt;
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.IP = res.Endpoint.VirtualIPs.First().Addr;
        container.Port = (int)res.Endpoint.Ports.First().PublishedPort;
        container.IsProxy = !_meta.ExposePort;

        if (!_meta.ExposePort)
            return container;

        container.PublicPort = container.Port;
        if (!string.IsNullOrEmpty(_meta.PublicEntry))
            container.PublicIP = _meta.PublicEntry;

        return container;
    }

    ServiceCreateParameters GetServiceCreateParameters(GZCTF.Models.Internal.ContainerConfig config)
    {
        // GZCTF_FLAG is Per-team dynamic flag issued & audited by the platform.
        //
        // Compliance & Abuse Notice:
        //
        // These env vars are integral to anti-abuse, audit trails and license compliance under
        // the Restricted License (LicenseRef-GZCTF-Restricted). Unauthorized removal, renaming
        // or semantic alteration can indicate an attempt to bypass license terms or weaken
        // challenge isolation guarantees. Downstream extensions MUST preserve their semantics.
        // Modification without a valid authorization may be treated as misuse.
        //
        // References: NOTICE, LICENSE_ADDENDUM.txt, licenses/LicenseRef-GZCTF-Restricted.txt
        IList<string> envs = config.Flag is null
            ? [$"GZCTF_TEAM_ID={config.TeamId}"]
            : [$"GZCTF_FLAG={config.Flag}", $"GZCTF_TEAM_ID={config.TeamId}"];

        return new()
        {
            RegistryAuth = _meta.AuthConfigs.GetForImage(config.Image),
            Service = new()
            {
                Name = DockerMetadata.GetName(config),
                Labels =
                    new Dictionary<string, string>
                    {
                        ["TeamId"] = config.TeamId,
                        ["UserId"] = config.UserId.ToString(),
                        ["ChallengeId"] = config.ChallengeId.ToString()
                    },
                Mode = new() { Replicated = new() { Replicas = 1 } },
                TaskTemplate = new()
                {
                    RestartPolicy = new() { Condition = "none" },
                    ContainerSpec =
                        new() { Image = config.Image, Env = envs, },
                    Resources = new()
                    {
                        Limits = new()
                        {
                            MemoryBytes = config.MemoryLimit * 1024 * 1024,
                            NanoCPUs = config.CPUCount * 1_0000_0000
                        }
                    }
                },
                EndpointSpec = new()
                {
                    Ports =
                    [
                        new()
                        {
                            PublishMode = _meta.ExposePort ? "global" : "vip", TargetPort = (uint)config.ExposedPort
                        }
                    ]
                }
            }
        };
    }
}
