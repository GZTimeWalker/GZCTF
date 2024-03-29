using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public partial class UpdateChecker(IDistributedCache cache, ILogger<UpdateChecker> logger, IOptions<UpdateCheckerOptions> options) : IHostedService
{
    [MemberNotNullWhen(true, nameof(LatestInformation))]
    internal static bool IsUpdateAvailable => LatestInformation?.AssemblyVersion is not null && Program.CurrentVersion is not null &&
                                              LatestInformation.AssemblyVersion > Program.CurrentVersion;

    internal static UpdateInformation? LatestInformation { get; private set; }

    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            UserAgent = { ProductInfoHeaderValue.Parse("request") },
            Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json"), MediaTypeWithQualityHeaderValue.Parse("text/html") }
        }
    };

    private readonly CancellationTokenSource _cts = new();

    private async Task UpdateCheckWorker(CancellationToken cancellationToken)
    {
        if (!options.Value.Enable || Program.CurrentVersion is null)
        {
            return;
        }

        var interval = TimeSpan.FromHours(options.Value.Interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UpdateInformation? lastCheck = null;

                if (await cache.GetAsync("GZCTF_UPDATE", cancellationToken) is [..] data)
                {
                    try
                    {
                        lastCheck = MemoryPackSerializer.Deserialize<UpdateInformation>(data);
                    }
                    catch
                    {
                        await cache.RemoveAsync("GZCTF_UPDATE", cancellationToken);
                    }
                }

                if (lastCheck is not null && DateTime.UtcNow - lastCheck.LastCheckTime < interval)
                {
                    LatestInformation = lastCheck;
                    continue;
                }

                var updateInfo = await JsonSerializer.DeserializeAsync<UpdateInformation>(
                    await _httpClient.GetStreamAsync("https://api.github.com/repos/GZTimeWalker/GZCTF/releases/latest",
                        cancellationToken), cancellationToken: cancellationToken);

                if (updateInfo is null)
                {
                    continue;
                }

                updateInfo.LastCheckTime = DateTime.UtcNow;
                updateInfo.AssemblyVersion = await FetchVersionAsync(updateInfo.TagName);

                await cache.SetAsync("GZCTF_UPDATE", MemoryPackSerializer.Serialize(updateInfo), new() { AbsoluteExpirationRelativeToNow = interval }, cancellationToken);

                LatestInformation = updateInfo;
            }
            catch (Exception e)
            {
                logger.LogError(e, Program.StaticLocalizer[nameof(Resources.Program.Update_CheckFailed)]);
            }
            finally
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Factory.StartNew(_ => UpdateCheckWorker(_cts.Token), null, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cts.Cancel();
        return Task.CompletedTask;
    }

    private static async Task<Version?> FetchVersionAsync(string tagName)
    {
        var projectContent = await _httpClient.GetStringAsync($"https://raw.githubusercontent.com/GZTimeWalker/GZCTF/{tagName}/src/GZCTF/GZCTF.csproj");
        var versionMatch = AssemblyVersionRegex().Match(projectContent);
        if (versionMatch.Success && versionMatch.Groups is [_, Group { Captures: [Capture { Value: var versionStr }] }])
        {
            var version = Version.Parse(versionStr);
            return new Version(version.Major is -1 ? 0 : version.Major,
                version.Minor is -1 ? 0 : version.Minor,
                version.Build is -1 ? 0 : version.Build,
                version.Revision is -1 ? 0 : version.Revision);
        }

        return null;
    }

    [GeneratedRegex("<AssemblyVersion>(.*?)</AssemblyVersion>", RegexOptions.CultureInvariant)]
    private static partial Regex AssemblyVersionRegex();
}

[MemoryPackable]
public partial class UpdateInformation
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime ReleaseTime { get; set; }

    [JsonIgnore]
    public Version? AssemblyVersion { get; set; }

    [JsonIgnore]
    public DateTime LastCheckTime { get; set; }
}

public class UpdateCheckerOptions
{
    public bool Enable { get; set; } = true;
    public int Interval { get; set; } = 6;
}
