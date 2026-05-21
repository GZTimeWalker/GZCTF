// SPDX-License-Identifier: LicenseRef-GZCTF-Restricted
// Copyright (C) 2022-2025 GZTimeWalker
// Restricted Component - NOT under AGPLv3.
// See licenses/LicenseRef-GZCTF-Restricted.txt

using System.Net;
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

    /// <summary>
    /// Sample point-in-time runtime stats (CPU / memory / network). Returns
    /// null when the underlying runtime doesn't expose stats (e.g. the
    /// Kubernetes implementation currently delegates this to metrics-server
    /// which isn't wired up), or when the container is no longer running.
    /// </summary>
    public Task<Models.Response.Admin.ContainerStatsModel?> GetStatsAsync(
        Models.Data.Container container, CancellationToken token = default);
}

internal static class ContainerManagerLogHelper
{
    private static void LogWithHttpContext<T>(
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

    extension<T>(ILogger<T> logger)
    {
        internal void LogCreationFailedWithHttpContext(string container,
            HttpStatusCode status,
            string body
        ) => LogWithHttpContext(logger, container, status, body,
            nameof(Resources.Program.ContainerManager_ContainerCreationFailedStatus),
            nameof(Resources.Program.ContainerManager_ContainerCreationFailedResponse));

        internal void LogDeletionFailedWithHttpContext(string container,
            HttpStatusCode status,
            string body
        ) => LogWithHttpContext(logger, container, status, body,
            nameof(Resources.Program.ContainerManager_ContainerDeletionFailedStatus),
            nameof(Resources.Program.ContainerManager_ContainerDeletionFailedResponse));

        internal void LogServiceCreationFailedWithHttpContext(string container,
            HttpStatusCode status,
            string body
        ) => LogWithHttpContext(logger, container, status, body,
            nameof(Resources.Program.ContainerManager_ServiceCreationFailedStatus),
            nameof(Resources.Program.ContainerManager_ServiceCreationFailedResponse));
    }
}
