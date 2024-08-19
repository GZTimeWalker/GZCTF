using System.Net;
using GZCTF.Models.Internal;

namespace GZCTF.Services.Container.Manager;

public interface IContainerManager
{
    /// <summary>
    /// 创建容器
    /// </summary>
    /// <param name="config">容器配置</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default);

    /// <summary>
    /// 销毁容器
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default);
}

static class ContainerManagerLogHelper
{
    static void LogWithHttpContext<T>(
        ILogger<T> logger,
        string container,
        HttpStatusCode status,
        string body,
        string statusLogFormatKey,
        string responseLogFormatKey
    )
    {
        logger.SystemLog(Program.StaticLocalizer[statusLogFormatKey, container, status],
            TaskStatus.Failed, LogLevel.Warning);
        logger.SystemLog(Program.StaticLocalizer[responseLogFormatKey, container, body],
            TaskStatus.Failed, LogLevel.Error);
    }

    internal static void LogCreationFailedWithHttpContext<T>(
        this ILogger<T> logger,
        string container,
        HttpStatusCode status,
        string body
    ) => LogWithHttpContext(logger, container, status, body,
        nameof(Resources.Program.ContainerManager_ContainerCreationFailedStatus),
        nameof(Resources.Program.ContainerManager_ContainerCreationFailedResponse));

    internal static void LogDeletionFailedWithHttpContext<T>(
        this ILogger<T> logger,
        string container,
        HttpStatusCode status,
        string body
    ) => LogWithHttpContext(logger, container, status, body,
        nameof(Resources.Program.ContainerManager_ContainerDeletionFailedStatus),
        nameof(Resources.Program.ContainerManager_ContainerDeletionFailedResponse));

    internal static void LogServiceCreationFailedWithHttpContext<T>(
        this ILogger<T> logger,
        string container,
        HttpStatusCode status,
        string body
    ) => LogWithHttpContext(logger, container, status, body,
        nameof(Resources.Program.ContainerManager_ServiceCreationFailedStatus),
        nameof(Resources.Program.ContainerManager_ServiceCreationFailedResponse));
}
