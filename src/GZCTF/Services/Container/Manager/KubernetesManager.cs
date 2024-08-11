/*
 * This file is protected and may not be modified without permission.
 * See LICENSE_ADDENDUM.txt for details.
 */

using System.Net;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace GZCTF.Services.Container.Manager;

public class KubernetesManager : IContainerManager
{
    readonly Kubernetes _client;
    readonly ILogger<KubernetesManager> _logger;
    readonly KubernetesMetadata _meta;

    public KubernetesManager(IContainerProvider<Kubernetes, KubernetesMetadata> provider,
        ILogger<KubernetesManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_K8sMode)],
            TaskStatus.Success,
            LogLevel.Debug);
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();
        var authSecretName = _meta.AuthSecretName;
        KubernetesConfig options = _meta.Config;

        if (imageName is null)
        {
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_UnresolvedImageName), config.Image],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        var chalImage = imageName.ToValidRFC1123String("chal");

        var name = $"{chalImage}-{Ulid.NewUlid().ToString().ToLowerInvariant()}";

        var pod = new V1Pod("v1", "Pod")
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = options.Namespace,
                Labels = new Dictionary<string, string>
                {
                    ["ctf.gzti.me/ResourceId"] = name,
                    ["ctf.gzti.me/Image"] = chalImage,
                    ["ctf.gzti.me/TeamId"] = config.TeamId,
                    ["ctf.gzti.me/UserId"] = config.UserId.ToString(),
                    ["ctf.gzti.me/ChallengeId"] = config.ChallengeId.ToString()
                }
            },
            Spec = new V1PodSpec
            {
                ImagePullSecrets =
                    authSecretName is null
                        ? Array.Empty<V1LocalObjectReference>()
                        : new List<V1LocalObjectReference> { new() { Name = authSecretName } },
                DnsPolicy = "None",
                DnsConfig = new() { Nameservers = options.Dns ?? ["8.8.8.8", "223.5.5.5", "114.114.114.114"] },
                EnableServiceLinks = false,
                Containers =
                [
                    new V1Container
                    {
                        Name = name,
                        Image = config.Image,
                        ImagePullPolicy = "Always",
                        // The GZCTF identifier is protected by the License.
                        // DO NOT REMOVE OR MODIFY THE FOLLOWING LINE.
                        // Please see LICENSE_ADDENDUM.txt for details.
                        Env =
                            config.Flag is null
                                ? [new V1EnvVar("GZCTF_TEAM_ID", config.TeamId)]
                                :
                                [
                                    new V1EnvVar("GZCTF_FLAG", config.Flag),
                                    new V1EnvVar("GZCTF_TEAM_ID", config.TeamId)
                                ],
                        Ports = [new V1ContainerPort(config.ExposedPort)],
                        Resources = new V1ResourceRequirements
                        {
                            Limits = new Dictionary<string, ResourceQuantity>
                            {
                                ["cpu"] = new($"{config.CPUCount * 100}m"),
                                ["memory"] = new($"{config.MemoryLimit}Mi"),
                                ["ephemeral-storage"] = new($"{config.StorageLimit}Mi")
                            },
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["cpu"] = new("10m"),
                                ["memory"] = new("32Mi")
                            }
                        }
                    }
                ],
                RestartPolicy = "Never",
                AutomountServiceAccountToken = false
            }
        };

        try
        {
            pod = await _client.CreateNamespacedPodAsync(pod, options.Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedStatus), name,
                    e.Response.StatusCode],
                TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailedResponse),
                    name,
                    e.Response.Content],
                TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerCreationFailed), name]);
            return null;
        }

        if (pod is null)
        {
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerInstanceCreationFailed),
                    config.Image.Split("/").LastOrDefault() ?? ""], TaskStatus.Failed,
                LogLevel.Warning);
            return null;
        }

        // Service is needed for port mapping
        var service = new V1Service("v1", "Service")
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = _meta.Config.Namespace,
                Labels = new Dictionary<string, string> { ["ctf.gzti.me/ResourceId"] = name }
            },
            Spec = new V1ServiceSpec
            {
                Type = _meta.ExposePort ? "NodePort" : "ClusterIP",
                Ports = [new V1ServicePort(config.ExposedPort, targetPort: config.ExposedPort)],
                Selector = new Dictionary<string, string> { ["ctf.gzti.me/ResourceId"] = name }
            }
        };

        try
        {
            service = await _client.CoreV1.CreateNamespacedServiceAsync(service, _meta.Config.Namespace,
                cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _client.CoreV1.DeleteNamespacedPodAsync(name, _meta.Config.Namespace, cancellationToken: token);
            }
            catch
            {
                // ignored
            }

            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ServiceCreationFailedStatus), name,
                    e.Response.StatusCode],
                TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ServiceCreationFailedResponse), name,
                    e.Response.Content],
                TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _client.CoreV1.DeleteNamespacedPodAsync(name, _meta.Config.Namespace, cancellationToken: token);
            }
            catch
            {
                // ignored
            }

            _logger.LogError(e,
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ServiceCreationFailed), name]);
            return null;
        }

        var container = new Models.Data.Container
        {
            ContainerId = name,
            Image = config.Image,
            Port = config.ExposedPort,
            IP = service.Spec.ClusterIP,
            IsProxy = !_meta.ExposePort,
            // No tracking for k8s-managed containers
            Status = ContainerStatus.Running
        };

        if (!_meta.ExposePort)
            return container;

        container.PublicIP = _meta.PublicEntry;
        container.PublicPort = service.Spec.Ports[0].NodePort;

        return container;
    }

    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            await _client.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, _meta.Config.Namespace,
                cancellationToken: token);
            await _client.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, _meta.Config.Namespace,
                cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                container.Status = ContainerStatus.Destroyed;
                return;
            }

            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailedStatus),
                    container.ContainerId,
                    e.Response.StatusCode], TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.ContainerManager_ContainerDeletionFailedResponse),
                    container.ContainerId,
                    e.Response.Content], TaskStatus.Failed, LogLevel.Error);
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
}
