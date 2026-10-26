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
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class DockerManager : IContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerManager> _logger;
    private readonly DockerMetadata _meta;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _imagePulledAt = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _imagePullLocks = new();

    public DockerManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

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
                _logger.LogDeletionFailedWithHttpContext(container.LogId, e.StatusCode, e.ResponseBody ?? string.Empty);
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

        if (!await EnsureImageAsync(config.Image, token))
            return null;

        var parameters = GetCreateContainerParameters(config);
        var containerName = parameters.Name ?? DockerMetadata.GetName(config);
        parameters.Name = containerName;

        if (_meta.ExposePort)
        {
            parameters.HostConfig ??= new();
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
                        containerName], TaskStatus.Failed, LogLevel.Information);
                return null;
            }

            containerRes = await _client.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            // the image is missing locally, pull it and retry
            if (!await PullImageAsync(config.Image, true, token))
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                        containerName], TaskStatus.Failed, LogLevel.Information);
                return null;
            }

            goto CreateDockerContainer;
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerExisted),
                        containerName],
                    TaskStatus.Duplicate,
                    LogLevel.Warning);

                // the container already exists, remove it and retry
                try
                {
                    await _client.Containers.RemoveContainerAsync(containerName,
                        new() { Force = true }, token);
                }
                catch (Exception ex)
                {
                    _logger.LogErrorMessage(ex,
                        StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                            containerName]);
                    return null;
                }

                goto CreateDockerContainer;
            }

            _logger.LogCreationFailedWithHttpContext(containerName, e.StatusCode, e.ResponseBody ?? string.Empty);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed),
                    containerName]);
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
        var state = info.State;

        if (state is null)
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    config.Image.Split("/").LastOrDefault() ?? "", string.Empty],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        container.Status = state.Dead || state.OOMKilled || state.Restarting
            ? ContainerStatus.Destroyed
            : state.Running
                ? ContainerStatus.Running
                : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    config.Image.Split("/").LastOrDefault() ?? "", state.Error],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(state.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        var networkSettings = info.NetworkSettings;
        container.IP = networkSettings?.Networks?.FirstOrDefault().Value?.IPAddress ?? string.Empty;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;

        if (!_meta.ExposePort)
            return container;

        var bindings = GetPublishedPortBindings(networkSettings?.Ports, config.ExposedPort);

        if (bindings is [])
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

    internal static IList<PortBinding> GetPublishedPortBindings(
        IDictionary<string, IList<PortBinding>>? ports, int exposedPort)
    {
        if (ports is not { Count: > 0 })
            return [];

        var port = exposedPort.ToString();
        var portPrefix = $"{port}/";
        var matchedPorts = ports
            .Where(kv => kv.Value is { Count: > 0 }
                && kv.Key.StartsWith(portPrefix, StringComparison.Ordinal))
            .ToArray();

        return matchedPorts switch
        {
            [] => [],
            [{ Value: var bindings }] => bindings,
            _ => matchedPorts.FirstOrDefault(kv =>
                     kv.Key.EndsWith("/tcp", StringComparison.OrdinalIgnoreCase))
                 .Value
                 ?? matchedPorts[0].Value
        };
    }

    private async Task<bool> EnsureImageAsync(string image, CancellationToken token)
    {
        if (IsImmutableReference(image) || !ShouldPullImage(image, DateTimeOffset.UtcNow))
            return true;

        return await PullImageAsync(image, false, token);
    }

    private async Task<bool> PullImageAsync(string image, bool force, CancellationToken token)
    {
        var key = NormalizeImageReference(image);
        var requestedAt = DateTimeOffset.UtcNow;
        var gate = _imagePullLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(token);
        try
        {
            var now = DateTimeOffset.UtcNow;

            // another request may have refreshed this image while we were waiting
            if (_imagePulledAt.TryGetValue(key, out var lastPulledAt) && lastPulledAt >= requestedAt)
                return true;

            if (!force && !ShouldPullImage(key, now))
                return true;

            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_PullContainerImage), image],
                TaskStatus.Pending, LogLevel.Information);

            var auth = _meta.AuthConfigs.GetForImage(image) ?? new AuthConfig();

            try
            {
                await _client.Images.CreateImageAsync(new() { FromImage = key }, auth,
                    new Progress<JSONMessage>(msg =>
                    {
                        Console.WriteLine($@"{msg.Status}|{msg.Progress}|{msg.Error}");
                    }), token);

                _imagePulledAt[key] = DateTimeOffset.UtcNow;
                return true;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                if (await LocalImageExistsAsync(image, token))
                {
                    // remember the failed check so queued callers and IfStale requests do not
                    // immediately hit the registry again while it is unavailable
                    _imagePulledAt[key] = DateTimeOffset.UtcNow;

                    _logger.LogWarning(e, "{msg:l}",
                        StaticLocalizer[
                            nameof(Resources.Program.ContainerManager_ImagePullFallbackToLocal), image]);
                    return true;
                }

                _logger.LogErrorMessage(e,
                    StaticLocalizer[nameof(Resources.Program.ContainerManager_ImagePullFailed), image]);
                return false;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private bool ShouldPullImage(string image, DateTimeOffset now) =>
        ShouldPullImage(_meta.Config.ImagePullPolicy,
            _imagePulledAt.TryGetValue(NormalizeImageReference(image), out var lastPulledAt)
                ? lastPulledAt
                : null,
            now,
            TimeSpan.FromMinutes(_meta.Config.ImagePullIntervalMinutes));

    internal static bool ShouldPullImage(ImagePullPolicy policy, DateTimeOffset? lastPulledAt,
        DateTimeOffset now, TimeSpan interval)
    {
        if (policy is ImagePullPolicy.Missing)
            return false;

        if (policy is ImagePullPolicy.Always || lastPulledAt is null || interval <= TimeSpan.Zero)
            return true;

        return now - lastPulledAt.Value >= interval;
    }

    internal static bool IsImmutableReference(string image) => image.Contains('@');

    internal static string NormalizeImageReference(string image)
    {
        if (IsImmutableReference(image))
            return image;

        var lastSlash = image.LastIndexOf('/');
        var lastSegment = lastSlash >= 0 ? image[(lastSlash + 1)..] : image;

        return lastSegment.Contains(':') ? image : $"{image}:latest";
    }

    private async Task<bool> LocalImageExistsAsync(string image, CancellationToken token)
    {
        try
        {
            await _client.Images.InspectImageAsync(image, token);
            return true;
        }
        catch (DockerImageNotFoundException)
        {
            return false;
        }
        catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
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
            Env = BuildContainerEnv(config),
            HostConfig = new()
            {
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                NetworkMode = _meta.NetworkNames[config.NetworkMode]
            }
        };

    private static IList<string> BuildContainerEnv(GZCTF.Models.Internal.ContainerConfig config)
    {
        var env = new List<string>(5)
        {
            $"GZCTF_TEAM_ID={config.TeamId}",
            $"GZCTF_USER_ID={config.UserId}",
            $"GZCTF_CHALLENGE_ID={config.ChallengeId}"
        };

        if (config.GameId is int gameId)
            env.Add($"GZCTF_GAME_ID={gameId}");

        if (!string.IsNullOrWhiteSpace(config.Flag))
            env.Add($"GZCTF_FLAG={config.Flag}");

        return env;
    }
}
