// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt

using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Services.Container.Provider;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class DockerManager : IContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerManager> _logger;
    private readonly DockerMetadata _meta;

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
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();

        if (string.IsNullOrWhiteSpace(imageName))
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_UnresolvedImageName), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

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
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_PullContainerImage), config.Image],
                TaskStatus.Pending, LogLevel.Information);

            var auth = _meta.AuthConfigs.GetForImage(config.Image) ?? new AuthConfig();

            // pull the image and retry
            await _client.Images.CreateImageAsync(new() { FromImage = config.Image }, auth,
                new Progress<JSONMessage>(msg =>
                {
                    Console.WriteLine($@"{msg.Status}|{msg.Progress}|{msg.Error}");
                }), token);

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
                var diag = await CaptureFailureDiagnosticsAsync(container.ContainerId, token);
                _logger.SystemLog(
                    StaticLocalizer[
                        nameof(Resources.Program.ContainerManager_ContainerInstanceStartFailed),
                        container.LogId,
                        config.Image.Split("/").LastOrDefault() ?? ""],
                    TaskStatus.Failed, LogLevel.Warning);
                if (!string.IsNullOrEmpty(diag))
                    _logger.SystemLog(diag, TaskStatus.Failed, LogLevel.Warning);

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
            var tail = await SafeFetchLogTailAsync(container.ContainerId, token);
            _logger.SystemLog(
                StaticLocalizer[
                    nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError),
                    config.Image.Split("/").LastOrDefault() ?? "", info.State.Error],
                TaskStatus.Failed, LogLevel.Warning);
            // Append the exit code + last stdout/stderr lines so the admin
            // can see WHY (vs. just "creation failed"). Containers that
            // exit 127 / 126 are usually CMD-not-found / not-executable;
            // OOM and SIGSEGV show up here too. Without this, the operator
            // has to ssh and `docker logs` to figure out what went wrong.
            _logger.SystemLog(
                $"Exit {info.State.ExitCode}: {info.State.Error ?? "(no error)"}; logs: {tail}",
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

    public async Task<Models.Response.Admin.ContainerStatsModel?> GetStatsAsync(
        Models.Data.Container container, CancellationToken token = default)
    {
        // Docker.DotNet's GetContainerStatsAsync overload that returns the
        // parsed model takes an IProgress callback. With Stream=false and
        // OneShot=true, the daemon emits a single sample and closes; the
        // progress callback fires once.
        ContainerStatsResponse? resp = null;
        var sink = new Progress<ContainerStatsResponse>(s => resp = s);
        try
        {
            await _client.Containers.GetContainerStatsAsync(
                container.ContainerId,
                new ContainerStatsParameters { Stream = false, OneShot = true },
                sink,
                token);
        }
        catch (DockerContainerNotFoundException) { return null; }
        catch (DockerApiException e) when (e.StatusCode == HttpStatusCode.NotFound) { return null; }
        catch (Exception e)
        {
            _logger.LogWarning(e, "DockerManager: GetStatsAsync failed for {Id}", container.LogId);
            return null;
        }
        if (resp is null) return null;

        // CPU %: classic Docker formula. Guard against the first read where
        // both deltas are zero (returns 0 instead of NaN).
        double cpu = 0;
        ulong cpuDelta = resp.CPUStats.CPUUsage.TotalUsage - resp.PreCPUStats.CPUUsage.TotalUsage;
        ulong sysDelta = resp.CPUStats.SystemUsage - resp.PreCPUStats.SystemUsage;
        uint onlineCpus = resp.CPUStats.OnlineCPUs;
        if (onlineCpus == 0 && resp.CPUStats.CPUUsage.PercpuUsage is { Count: > 0 } perc)
            onlineCpus = (uint)perc.Count;
        if (sysDelta > 0 && onlineCpus > 0)
            cpu = (double)cpuDelta / sysDelta * onlineCpus * 100.0;

        long memUsed = (long)resp.MemoryStats.Usage;
        long memLimit = (long)resp.MemoryStats.Limit;

        long rx = 0, tx = 0;
        if (resp.Networks is { } nets)
        {
            foreach (var kv in nets)
            {
                rx += (long)kv.Value.RxBytes;
                tx += (long)kv.Value.TxBytes;
            }
        }

        return new Models.Response.Admin.ContainerStatsModel
        {
            CpuPercent = Math.Round(cpu, 2),
            MemoryUsedBytes = memUsed,
            MemoryLimitBytes = memLimit,
            NetRxBytes = rx,
            NetTxBytes = tx
        };
    }

    /// <summary>
    /// Best-effort: inspect the container + read its last stdout/stderr
    /// lines so the failure log includes WHY the container died (e.g.
    /// "exit 127: applet not found" for a missing busybox component, or
    /// "OOMKilled" for memory pressure). All errors are swallowed —
    /// the diagnostic should never block the destroy path.
    /// </summary>
    private async Task<string> CaptureFailureDiagnosticsAsync(string containerId, CancellationToken token)
    {
        try
        {
            var info = await _client.Containers.InspectContainerAsync(containerId, token);
            var tail = await SafeFetchLogTailAsync(containerId, token);
            return $"start failed: exit {info.State.ExitCode}, dead={info.State.Dead}, oom={info.State.OOMKilled}, error='{info.State.Error}'; logs: {tail}";
        }
        catch (Exception e)
        {
            return $"start failed (diagnostics unavailable: {e.Message})";
        }
    }

    private async Task<string> SafeFetchLogTailAsync(string containerId, CancellationToken token)
    {
        try
        {
            var buf = new System.Text.StringBuilder(2048);
            var progress = new Progress<string>(line =>
            {
                if (line is null) return;
                if (buf.Length > 2048) return;
                buf.Append(line);
                if (!line.EndsWith('\n')) buf.Append('\n');
            });
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await _client.Containers.GetContainerLogsAsync(containerId,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = "20"
                }, progress, cts.Token);
            var s = buf.ToString().Replace('\n', ' ').Replace('\r', ' ').Trim();
            return s.Length > 1024 ? s[..1024] + "…" : (string.IsNullOrEmpty(s) ? "(empty)" : s);
        }
        catch (Exception e)
        {
            return $"(log fetch failed: {e.Message})";
        }
    }

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
