// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt

using System.Collections.Concurrent;
using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using Microsoft.Extensions.Options;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class DockerManager : IContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerManager> _logger;
    private readonly DockerMetadata _meta;
    private readonly SemaphoreSlim _createSemaphore;
    private readonly ConcurrentDictionary<string, byte> _readyImages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _imagePullLocks = new(StringComparer.OrdinalIgnoreCase);

    public DockerManager(
        IContainerProvider<DockerClient, DockerMetadata> provider,
        IOptions<ContainerPolicy> containerPolicy,
        ILogger<DockerManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();
        var maxConcurrency = Math.Clamp(containerPolicy.Value.MaxConcurrentContainerStarts, 1, 64);
        _createSemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerMode)],
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
                _logger.LogDeletionFailedWithHttpContext(container.LogId, e.StatusCode, e.ResponseBody);
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
        await _createSemaphore.WaitAsync(token);
        try
        {
            var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();

            if (string.IsNullOrWhiteSpace(imageName))
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_UnresolvedImageName), config.Image],
                    TaskStatus.Failed, LogLevel.Warning);
                return null;
            }

            if (!await EnsureImageReadyAsync(config.Image, token))
                return null;

            var parameters = GetCreateContainerParameters(config);

            if (_meta.ExposePort)
            {
                parameters.ExposedPorts = new Dictionary<string, EmptyStruct> { [config.ExposedPort.ToString()] = new() };
                parameters.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    // let docker choose a random port, do not use "PublishAllPorts" option
                    // reference: https://github.com/moby/moby/blob/master/daemon/libnetwork/portallocator/portallocator.go#L135
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
                        StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                            parameters.Name], TaskStatus.Failed, LogLevel.Information);
                    return null;
                }

                containerRes = await _client.Containers.CreateContainerAsync(parameters, token);
            }
            catch (DockerImageNotFoundException)
            {
                if (!await EnsureImageReadyAsync(config.Image, token))
                    return null;

                goto CreateDockerContainer;
            }
            catch (DockerApiException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                {
                    _logger.SystemLog(
                        StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted),
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
                        _logger.LogErrorMessage(ex,
                            StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                                parameters.Name]);
                        return null;
                    }

                    goto CreateDockerContainer;
                }

                _logger.LogCreationFailedWithHttpContext(parameters.Name, e.StatusCode, e.ResponseBody);
                return null;
            }
            catch (Exception e)
            {
                _logger.LogErrorMessage(e,
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
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
                        StaticLocalizer[
                            nameof(Resources.Program.ContainerManager_ContainerInstanceStartFailed),
                            container.LogId,
                            config.Image.Split("/").LastOrDefault() ?? ""],
                        TaskStatus.Failed, LogLevel.Warning);

                    await DestroyContainerAsync(container, token);
                    return null;
                }

                var started = await _client.Containers.StartContainerAsync(container.ContainerId,
                    new(), token);

                if (started)
                    break;

                await Task.Delay(500, token);
            }

            var info = await _client.Containers.InspectContainerAsync(container.ContainerId, token);

            container.Status = info.State.Dead || info.State.OOMKilled || info.State.Restarting
                ? ContainerStatus.Destroyed
                : info.State.Running
                    ? ContainerStatus.Running
                    : ContainerStatus.Pending;

            if (container.Status != ContainerStatus.Running)
            {
                _logger.SystemLog(
                    StaticLocalizer[
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

        var portString = config.ExposedPort.ToString();
        var bindings = info.NetworkSettings.Ports.Where(kv => kv.Key.StartsWith(portString)).Select(kv => kv.Value)
            .SingleOrDefault();

        if (bindings is not { Count: > 0 })
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                    config.Image.Split("/").LastOrDefault() ?? ""],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        var port = bindings.First().HostPort;

        if (int.TryParse(port, out var numPort))
            container.PublicPort = numPort;
        else
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_PortParsingFailed), port],
                TaskStatus.Failed,
                LogLevel.Warning);

            if (!string.IsNullOrEmpty(_meta.PublicEntry))
                container.PublicIP = _meta.PublicEntry;

            return container;
        }
        finally
        {
            _createSemaphore.Release();
        }
    }

    private async Task<bool> EnsureImageReadyAsync(string image, CancellationToken token)
    {
        if (_readyImages.ContainsKey(image))
            return true;

        var pullLock = _imagePullLocks.GetOrAdd(image, _ => new SemaphoreSlim(1, 1));
        await pullLock.WaitAsync(token);

        try
        {
            if (_readyImages.ContainsKey(image))
                return true;

            try
            {
                await _client.Images.InspectImageAsync(image, token);
                _readyImages[image] = 0;
                return true;
            }
            catch (DockerImageNotFoundException)
            {
                // Pull below
            }
            catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Pull below
            }

            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_PullContainerImage), image],
                TaskStatus.Pending, LogLevel.Information);

            var auth = _meta.AuthConfigs.GetForImage(image);
            await _client.Images.CreateImageAsync(new() { FromImage = image }, auth,
                new Progress<JSONMessage>(msg =>
                {
                    var err = msg.Error?.Message;
                    if (!string.IsNullOrWhiteSpace(err))
                        _logger.LogWarning("Docker image pull error: {error}", err);
                }), token);

            await _client.Images.InspectImageAsync(image, token);
            _readyImages[image] = 0;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogErrorMessage(ex,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), image]);
            return false;
        }
        finally
        {
            pullLock.Release();
        }
    }

    private CreateContainerParameters GetCreateContainerParameters(GZCTF.Models.Internal.ContainerConfig config) =>
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
            Env = config.Flag is null
                ? [$"GZCTF_TEAM_ID={config.TeamId}"]
                : [$"GZCTF_FLAG={config.Flag}", $"GZCTF_TEAM_ID={config.TeamId}"],
            HostConfig = new()
            {
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                NetworkMode = _meta.NetworkNames[config.NetworkMode]
            }
        };
}
