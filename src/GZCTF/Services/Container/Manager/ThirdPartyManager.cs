using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using GZCTF.Services.Container.ThirdParty;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class ThirdPartyManager : IContainerManager
{
    private readonly IThirdPartyClient _client;
    private readonly ILogger<ThirdPartyManager> _logger;
    private readonly ThirdPartyMetadata _meta;

    public ThirdPartyManager(IContainerProvider<IThirdPartyClient, ThirdPartyMetadata> provider,
        ILogger<ThirdPartyManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        _logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerManager_ThirdPartyMode)],
            TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        try
        {
            var payload = await _client.CreateAsync(config, requestId, token);
            return BuildContainerFromResponse(config, payload);
        }
        catch (ThirdPartyRequestException e)
        {
            _logger.LogCreationFailedWithHttpContext(config.Image, e.StatusCode, e.Body);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), config.Image]);
            return null;
        }
    }

    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            var destroyed = await _client.DestroyAsync(container.ContainerId, token);
            if (destroyed)
                container.Status = ContainerStatus.Destroyed;
            return;
        }
        catch (ThirdPartyRequestException e)
        {
            _logger.LogDeletionFailedWithHttpContext(container.LogId, e.StatusCode, e.Body);
            return;
        }
        catch (Exception e)
        {
            _logger.LogErrorMessage(e,
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailed),
                    container.LogId]);
            return;
        }
    }

    private Models.Data.Container? BuildContainerFromResponse(ContainerConfig config,
        IThirdPartyCreateResponse? payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        if (payload.State == ThirdPartyContainerState.Failed)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        if (payload.InternalAddress is null || string.IsNullOrWhiteSpace(payload.InternalAddress.Ip))
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        if (_meta.ExposePort && payload.ExternalAddress is null)
        {
            _logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerPortNotExposed), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var container = new Models.Data.Container
        {
            ContainerId = payload.Id,
            Image = config.Image,
            IP = payload.InternalAddress.Ip,
            Port = payload.InternalAddress.Port,
            IsProxy = !_meta.ExposePort,
            Status = ContainerStatus.Running,
            StartedAt = payload.StartedAt ?? now,
            ExpectStopAt = payload.ExpectStopAt ?? now + TimeSpan.FromHours(2)
        };

        if (payload.ExternalAddress is not null)
        {
            container.PublicIP = payload.ExternalAddress.Ip;
            container.PublicPort = payload.ExternalAddress.Port;
        }

        return container;
    }
}
