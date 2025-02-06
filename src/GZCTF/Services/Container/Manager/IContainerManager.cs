﻿using System.Net;
using GZCTF.Models.Internal;

namespace GZCTF.Services.Container.Manager;

public interface IContainerManager
{
    /// <summary>
    /// Create a container
    /// </summary>
    /// <param name="config">container configuration</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default);

    /// <summary>
    /// Destroy a container
    /// </summary>
    /// <param name="container">container</param>
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
        logger.SystemLog(StaticLocalizer[statusLogFormatKey, container, status],
            TaskStatus.Failed, LogLevel.Warning);
        logger.SystemLog(StaticLocalizer[responseLogFormatKey, container, body],
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
