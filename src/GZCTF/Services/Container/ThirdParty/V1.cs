using System.Text.Json;
using GZCTF.Models.Internal;

namespace GZCTF.Services.Container.ThirdParty;

public static class ThirdPartyV1
{
    public const string RequestIdHeader = "X-Request-Id";
    public const string HealthPath = "/v1/healthz";
    public const string CreatePath = "/v1/containers";
    public const string DestroyPath = "/v1/containers/{id}";

    public sealed class CreateRequest
    {
        public string Image { get; set; } = string.Empty;
        public int ExposedPort { get; set; }
        public Resources Resources { get; set; } = new();
        public NetworkMode NetworkMode { get; set; } = NetworkMode.Open;
        public Labels Labels { get; set; } = new();
        public Dictionary<string, string> Env { get; set; } = new();
    }

    public sealed class Resources
    {
        public double Cpu { get; set; }
        public int Memory { get; set; }
        public int Storage { get; set; }
    }

    public sealed class Labels
    {
        public string TeamId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int ChallengeId { get; set; }
    }

    public sealed class CreateResponse : IThirdPartyCreateResponse
    {
        public string? Id { get; set; }
        public ThirdPartyContainerState? State { get; set; }
        public ThirdPartyAddress? InternalAddress { get; set; }
        public ThirdPartyAddress? ExternalAddress { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? ExpectStopAt { get; set; }
    }

    public sealed class Protocol : IThirdPartyProtocol
    {
        public ThirdPartyApiVersion Version => ThirdPartyApiVersion.V1;
        public string RequestIdHeader => ThirdPartyV1.RequestIdHeader;
        public string CreatePath => ThirdPartyV1.CreatePath;
        public string DestroyPath => ThirdPartyV1.DestroyPath;

        public object BuildCreateRequest(ContainerConfig config)
        {
            var env = new Dictionary<string, string> { ["GZCTF_TEAM_ID"] = config.TeamId };
            if (!string.IsNullOrWhiteSpace(config.Flag))
                env["GZCTF_FLAG"] = config.Flag;

            return new CreateRequest
            {
                Image = config.Image,
                ExposedPort = config.ExposedPort,
                Resources = new Resources
                {
                    Cpu = config.CPUCount / 10.0,
                    Memory = config.MemoryLimit,
                    Storage = config.StorageLimit
                },
                NetworkMode = config.NetworkMode,
                Labels = new Labels
                {
                    TeamId = config.TeamId,
                    UserId = config.UserId,
                    ChallengeId = config.ChallengeId
                },
                Env = env
            };
        }

        public IThirdPartyCreateResponse? DeserializeCreateResponse(string json, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<CreateResponse>(json, options);
    }
}
