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

public class DockerManager : IContainerManager
{
    readonly DockerClient _client;
    readonly ILogger<DockerManager> _logger;
    readonly DockerMetadata _meta;

    public DockerManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerMode)],
            TaskStatus.Success, LogLevel.Debug);
    }


    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(container.ContainerId,
                new() { Force = true }, token);
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
            _logger.LogError(e,
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    container.ContainerId]);
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        CreateContainerParameters parameters = GetCreateContainerParameters(config);

        if (_meta.ExposePort)
        {
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct> { [config.ExposedPort.ToString()] = new() };
            parameters.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                // let docker choose a random port, do not use "PublishAllPorts" option
                // reference: https://github.com/moby/moby/blob/master/libnetwork/portallocator/portallocator.go
                // function: RequestPortsInRange
                // comment:
                //     If portStart and portEnd are 0 it returns
                //     the first free port in the default ephemeral range.
                [config.ExposedPort.ToString()] = [new PortBinding { HostPort = "0" }]
            };
        }

        CreateContainerResponse? containerRes;
        var retry = 0;

    CreateDockerContainer:
        try
        {
            if (retry++ >= 3)
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                        parameters.Name], TaskStatus.Failed, LogLevel.Information);
                return null;
            }

            containerRes = await _client.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_PullContainerImage), config.Image],
                TaskStatus.Pending, LogLevel.Information);

            // pull the image and retry
            await _client.Images.CreateImageAsync(new() { FromImage = config.Image }, _meta.Auth,
                new Progress<JSONMessage>(msg =>
                {
                    Console.WriteLine($@"{msg.Status}|{msg.ProgressMessage}|{msg.ErrorMessage}");
                }), token);

            goto CreateDockerContainer;
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted),
                        parameters.Name],
                    TaskStatus.Duplicate,
                    LogLevel.Warning);

                // the container already exists, remove it and retry
                try
                {
                    await _client.Containers.RemoveContainerAsync(parameters.Name,
                        new() { Force = true }, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{msg}",
                        Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                            parameters.Name]);
                    return null;
                }

                goto CreateDockerContainer;
            }

            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedStatus),
                    parameters.Name,
                    e.StatusCode], TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedResponse),
                    parameters.Name,
                    e.ResponseBody], TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{msg}",
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                    parameters.Name]);
            return null;
        }

        var container = new Models.Data.Container { ContainerId = containerRes.ID, Image = config.Image };

        retry = 0;

        while (true)
        {
            if (retry++ >= 3)
            {
                _logger.SystemLog(
                    Program.StaticLocalizer[
                        nameof(Resources.Program.ContainerManager_ContainerInstanceStartFailed),
                        container.ContainerId[..12],
                        config.Image.Split("/").LastOrDefault() ?? ""],
                    TaskStatus.Failed, LogLevel.Warning);
                return null;
            }

            var started = await _client.Containers.StartContainerAsync(container.ContainerId,
                new(), token);

            if (started)
                break;

            await Task.Delay(500, token);
        }

        ContainerInspectResponse? info = await _client.Containers.InspectContainerAsync(container.ContainerId, token);

        container.Status = info.State.Dead || info.State.OOMKilled || info.State.Restarting
            ? ContainerStatus.Destroyed
            : info.State.Running
                ? ContainerStatus.Running
                : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog(
                Program.StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    config.Image.Split("/").LastOrDefault() ?? "", info.State.Error],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.IP = info.NetworkSettings.Networks.FirstOrDefault().Value.IPAddress;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;

        if (!_meta.ExposePort)
            return container;

        var port = info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString())
            ).Value.First().HostPort;

        if (int.TryParse(port, out var numPort))
            container.PublicPort = numPort;
        else
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_PortParsingFailed), port],
                TaskStatus.Failed,
                LogLevel.Warning);

        if (!string.IsNullOrEmpty(_meta.PublicEntry))
            container.PublicIP = _meta.PublicEntry;

        return container;
    }

    CreateContainerParameters GetCreateContainerParameters(ContainerConfig config) =>
        new()
        {
            Image = config.Image,
            Labels =
                new Dictionary<string, string>
                {
                    ["TeamId"] = config.TeamId,
                    ["UserId"] = config.UserId.ToString(),
                    ["ChallengeId"] = config.ChallengeId.ToString()
                },
            Name = DockerMetadata.GetName(config),
            // The GZCTF identifier is protected by the License.
            // DO NOT REMOVE OR MODIFY THE FOLLOWING LINE.
            // Please see LICENSE_ADDENDUM.txt for details.
            Env = config.Flag is null
                ? [$"GZCTF_TEAM_ID={config.TeamId}"]
                : [$"GZCTF_FLAG={config.Flag}", $"GZCTF_TEAM_ID={config.TeamId}"],
            HostConfig = new()
            {
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                NetworkMode = _meta.Config.ChallengeNetwork
            }
        };
}
