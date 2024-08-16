/*
 * This file is protected and may not be modified without permission.
 * See LICENSE_ADDENDUM.txt for details.
 */

using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

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

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_SwarmMode)],
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
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                    container.ContainerId],
                TaskStatus.Success, LogLevel.Debug);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDestroyed),
                        container.ContainerId],
                    TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailedStatus),
                        container.ContainerId,
                        e.StatusCode], TaskStatus.Failed, LogLevel.Warning);
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailedResponse),
                        container.ContainerId,
                        e.ResponseBody], TaskStatus.Failed, LogLevel.Error);
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    container.ContainerId]);
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        ServiceCreateParameters parameters = GetServiceCreateParameters(config);
        ServiceCreateResponse? serviceRes;
        var retry = 0;

    CreateContainer:
        try
        {
            if (retry++ >= 3)
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
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
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted),
                        parameters.Service.Name],
                    TaskStatus.Duplicate,
                    LogLevel.Warning);

                await _client.Swarm.RemoveServiceAsync(parameters.Service.Name, token);

                goto CreateContainer;
            }

            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedStatus),
                    parameters.Service.Name,
                    e.StatusCode], TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedResponse),
                    parameters.Service.Name,
                    e.ResponseBody], TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
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
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerPortNotExposed),
                        container.ContainerId],
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

    ServiceCreateParameters GetServiceCreateParameters(ContainerConfig config) =>
        new()
        {
            RegistryAuth = _meta.Auth,
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
                        new()
                        {
                            Image = config.Image,
                            // The GZCTF identifier is protected by the License.
                            // DO NOT REMOVE OR MODIFY THE FOLLOWING LINE.
                            // Please see LICENSE_ADDENDUM.txt for details.
                            Env =
                                config.Flag is null
                                    ? [$"GZCTF_TEAM_ID={config.TeamId}"]
                                    : [$"GZCTF_FLAG={config.Flag}", $"GZCTF_TEAM_ID={config.TeamId}"],
                        },
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
                            PublishMode = _meta.ExposePort ? "global" : "vip",
                            TargetPort = (uint)config.ExposedPort
                        }
                    ]
                }
            }
        };
}
