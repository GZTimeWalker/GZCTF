using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Docker.DotNet;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class DockerComposeManager : IContainerManager
{
    readonly DockerClient _client;
    readonly ILogger<DockerComposeManager> _logger;
    readonly DockerMetadata _meta;
    readonly DockerManager _fallbackManager;

    public DockerComposeManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerComposeManager> logger, ILogger<DockerManager> fallbackLogger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();
        _fallbackManager = new DockerManager(provider, fallbackLogger);

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerComposeMode)], TaskStatus.Success, LogLevel.Debug);
    }


    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        if (!container.Image.Trim('\n', '\r').Contains('\n'))
        {
            await _fallbackManager.DestroyContainerAsync(container, token);
            return;
        }

        try
        {
            using TempDir tempDir = new TempDir("gzdockertmp_");
            using TempDir loginDir = new TempDir();

            await RunCommand("docker",
                ["compose", "--file", "-", "--project-name", container.ContainerId, "--progress", "plain", "down", "--remove-orphans", "--volumes"],
                new Dictionary<string, string?> { { "DOCKER_HOST", _meta.Config.Uri }, { "DOCKER_CONFIG", loginDir.ToString() } },
                tempDir.ToString(), container.Image, token);
        }
        catch (LaunchException le)
        {
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_ComposeDeletionFailedResponse), container.ContainerId, le.ExitCode, le.Message],
                TaskStatus.Failed, LogLevel.Error);
            return;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e, StaticLocalizer[nameof(Resources.Program.ContainerManager_ComposeDeletionFailed), container.ContainerId]);
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0037")]
    private async Task GenerateComposeOverride(string filename, List<string> services, ContainerConfig config, CancellationToken token = default)
    {
        var compose = new
        {
            services = new Dictionary<string, object>(),
            networks = new Dictionary<string, object>(),
        };

        var networks = new Dictionary<string, object?>();

        if (!String.IsNullOrWhiteSpace(_meta.Config.ChallengeNetwork))
        {
            compose.networks[_meta.Config.ChallengeNetwork] = new
            {
                name = _meta.Config.ChallengeNetwork,
                external = true,
            };

            networks[_meta.Config.ChallengeNetwork] = null;
        }

        var labels = new
        {
            TeamId = config.TeamId,
            UserId = config.UserId.ToString(),
            ChallengeId = config.ChallengeId.ToString()
        };

        var mainService = new Dictionary<string, object>
        {
            { "ports", new[] { $"0:{config.ExposedPort}" } },
            { "labels", labels },
            { "mem_limit", $"{config.MemoryLimit}M" },
            { "cpus", config.CPUCount / 10.0 },
            //// This only works if the backing storage is xfs+pquota, maybe try to query it somehow?
            // { "storage_opt", new { size = $"{config.StorageLimit}M" } },
        };

        if (networks.Count != 0)
            mainService["networks"] = networks;

        compose.services.Add("main", mainService);

        foreach (var service in services)
        {
            if (service == "main")
                continue;

            var serviceObj = new Dictionary<string, object>
            {
                { "labels", labels },
                { "mem_limit", $"{config.MemoryLimit}M" },
                { "cpus", config.CPUCount / 10.0 },
                //// This only works if the backing storage is xfs+pquota, maybe try to query it somehow?
                // { "storage_opt", new { size = $"{config.StorageLimit}M" } },
            };

            if (networks.Count != 0)
                serviceObj["networks"] = networks;

            compose.services.Add(service, serviceObj);
        }

        string json = JsonSerializer.Serialize(compose);
        await File.WriteAllTextAsync(filename, json, token);
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        // if the image is just a single line, assume it's a classic docker image and use the normal manager
        if (!config.Image.Trim('\n', '\r').Contains('\n'))
            return await _fallbackManager.CreateContainerAsync(config, token);

        string name = $"{config.TeamId}_{config.ChallengeId}_{(config.Flag ?? Guid.NewGuid().ToString("N")).ToMD5String()[..16]}";
        using TempDir tempDir = new TempDir("gzdockertmp_");
        using TempDir loginDir = new TempDir();
        var defaultEnv = new Dictionary<string, string?> { { "DOCKER_HOST", _meta.Config.Uri }, { "DOCKER_CONFIG", loginDir.ToString() } };

        List<string> services;

        try
        {
            services = await RunCommand("docker", ["compose", "--file", "-", "--project-name", name, "config", "--services"],
                defaultEnv, tempDir.ToString(), config.Image, token);

            if (!services.Contains("main"))
            {
                _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_ComposeNoMainService), config.ChallengeId], TaskStatus.Failed, LogLevel.Warning);
                return null;
            }

            var images = await RunCommand("docker", ["compose", "--file", "-", "--project-name", name, "config", "--images"],
                defaultEnv, tempDir.ToString(), config.Image, token);

            foreach (var image in images)
            {
                var auth = _meta.AuthConfigs.GetForImage(image, out var registry);
                if (auth is null)
                    continue;

                await RunCommand("docker", ["login", "--password-stdin", "--username", auth.Username, registry],
                    defaultEnv, tempDir.ToString(), auth.Password, token);
            }
        }
        catch (LaunchException le)
        {
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerCommandFailed), le.ExitCode, le.Message],
                TaskStatus.Failed, LogLevel.Error);
            return null;
        }

        string preludeFile = Path.Combine(tempDir.Path.ToString(), "override.json");
        await GenerateComposeOverride(preludeFile, services, config, token);

        try
        {
            await RunCommand("docker",
                ["compose", "--file", preludeFile, "--file", "-", "--project-name", name, "--progress", "plain", "up", "-d", "--wait", "--pull", "missing"],
                new Dictionary<string, string?>
                {
                { "DOCKER_HOST", _meta.Config.Uri },
                { "DOCKER_CONFIG", loginDir.ToString() },
                { "CPU_COUNT", (config.CPUCount / 10.0).ToString() },
                { "MEM_LIMIT", (config.MemoryLimit * 1024 * 1024).ToString() },
                { "NET_MODE", _meta.Config.ChallengeNetwork ?? "default" },
                { "GZCTF_TEAM_ID", config.TeamId },
                { "GZCTF_FLAG", config.Flag },
                },
                tempDir.ToString(), config.Image, token);
        }
        catch (LaunchException le)
        {
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_ComposeCreationFailed), config.ChallengeId, le.ExitCode, le.Message],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        Models.Data.Container container = new Models.Data.Container { ContainerId = name, Image = config.Image };
        string mainId;

        try
        {
            var mainIds = await RunCommand("docker", ["compose", "--file", "-", "--project-name", name, "ps", "-q", "main"],
                defaultEnv, tempDir.ToString(), config.Image, token);
            if (mainIds.Count != 1)
            {
                _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_ComposeUnexpectedIDs)],
                    TaskStatus.Failed, LogLevel.Error);

                await DestroyContainerAsync(container, token);
                return null;
            }

            mainId = mainIds[0];
        }
        catch (LaunchException le)
        {
            _logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerManager_DockerCommandFailed), le.ExitCode, le.Message],
                TaskStatus.Failed, LogLevel.Error);

            await DestroyContainerAsync(container, token);
            return null;
        }

        var info = await _client.Containers.InspectContainerAsync(mainId, token);

        container.IP = info.NetworkSettings.Networks.FirstOrDefault().Value.IPAddress;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;
        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);

        container.Status = info.State.Dead || info.State.OOMKilled || info.State.Restarting
            ? ContainerStatus.Destroyed
            : info.State.Running
                ? ContainerStatus.Running
                : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailedWithError), "Compose Script", info.State.Error],
                TaskStatus.Failed, LogLevel.Warning);

            await DestroyContainerAsync(container, token);
            return null;
        }

        if (!_meta.ExposePort)
            return container;

        var port = info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString() + "/") || p.Key == config.ExposedPort.ToString()
            ).Value.First().HostPort;

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

    private async Task<List<string>> RunCommand(string command, string[] arguments, Dictionary<string, string?>? env = null, string workdir = "", string? input = null, CancellationToken token = default)
    {
        command = ResolvePath(command);

        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            WorkingDirectory = workdir,
        };

        foreach (string arg in arguments)
            proc.StartInfo.ArgumentList.Add(arg);

        foreach (var pair in env ?? [])
            if (pair.Value != null)
                proc.StartInfo.Environment.Add(pair.Key, pair.Value);

        proc.EnableRaisingEvents = true;

        List<string> res = new List<string>();
        DataReceivedEventHandler handler = (sender, data) => { if (data.Data != null) res.Add(data.Data); };
        proc.OutputDataReceived += handler;
        proc.ErrorDataReceived += handler;

        try
        {
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using (var writer = proc.StandardInput)
                if (input != null)
                    await writer.WriteAsync(input);

            await proc.WaitForExitAsync(token);

            if (proc.ExitCode != 0)
                throw new LaunchException(proc.ExitCode, $"{command} {String.Join(' ', arguments)}{Environment.NewLine}{String.Join(Environment.NewLine, res)}");

            return res;
        }
        finally
        {
            proc.OutputDataReceived -= handler;
            proc.ErrorDataReceived -= handler;
        }
    }

    private Dictionary<string, string> _resolvedCommands = [];

    private string ResolvePath(string command)
    {
        if (_resolvedCommands.TryGetValue(command, out var res))
            return res;

        if (!Path.IsPathRooted(command) && !command.Contains(Path.DirectorySeparatorChar) && !command.Contains(Path.AltDirectorySeparatorChar) && !File.Exists(command))
        {
            string[] path = Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator);
            string[] exts = Environment.GetEnvironmentVariable("PATHEXT")?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            foreach (string dir in path)
            {
                string fullCommand = Path.Combine(dir, command);
                if (File.Exists(fullCommand))
                {
                    _resolvedCommands.Add(command, fullCommand);
                    return fullCommand;
                }

                foreach (string ext in exts)
                {
                    string fullCommandExt = fullCommand + ext;
                    if (File.Exists(fullCommandExt))
                    {
                        _resolvedCommands.Add(command, fullCommandExt);
                        return fullCommandExt;
                    }
                }
            }
        }

        return command;
    }

    private class TempDir : IDisposable
    {
        public DirectoryInfo Path { get; private set; }
        public override string ToString() => Path.FullName;

        public TempDir(string? prefix = null)
        {
            Path = Directory.CreateTempSubdirectory(prefix);
        }

        public void Dispose()
        {
            if (Path.Exists)
                Path.Delete(true);
        }
    }

    private class LaunchException : Exception
    {
        public int ExitCode { get; private set; }

        public LaunchException(int code, string message)
            : base(message)
        {
            ExitCode = code;
        }
    }
}
