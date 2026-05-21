using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Services.Container.Provider;

namespace GZCTF.Services.Container.Build;

/// <summary>
/// Builds challenge images via Docker.DotNet against the mounted
/// <c>docker.sock</c>. Tags are deterministic
/// (<c>gzctf-auto/{gameId}/{slug}:{contextSha[..12]}</c>) so re-imports
/// with no source changes reuse the existing image without rebuilding.
///
/// No registry push — the same daemon GZCTF talks to is the one the
/// runner uses, so locally tagged images are immediately available.
/// </summary>
public sealed class DockerChallengeImageBuilder(
    IContainerProvider<DockerClient, DockerMetadata> provider,
    ILogger<DockerChallengeImageBuilder> logger) : IChallengeImageBuilder
{
    private readonly DockerClient _client = provider.GetProvider();
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(5);
    private const int LogTailBytes = 32 * 1024;

    public async Task<ChallengeBuildResult> BuildAsync(ChallengeBuildRequest req, CancellationToken token)
    {
        var slug = NormalizeSlug(req.ChallengeSlug);
        var contextTar = Path.Combine(Path.GetTempPath(), $"gzctf-build-{Guid.NewGuid():N}.tar.gz");

        try
        {
            // Tar+gzip the context dir, computing SHA256 over the
            // compressed bytes as we go for tag determinism.
            using var sha = SHA256.Create();
            await using (var fs = File.Create(contextTar))
            await using (var hashing = new CryptoStream(fs, sha, CryptoStreamMode.Write, leaveOpen: false))
            await using (var gz = new GZipStream(hashing, CompressionLevel.Fastest, leaveOpen: false))
            await using (var tar = new TarWriter(gz, leaveOpen: false))
            {
                foreach (var f in Directory.EnumerateFiles(req.ContextDir, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(req.ContextDir, f).Replace('\\', '/');
                    if (rel.StartsWith("..", StringComparison.Ordinal)) continue;
                    var entry = new PaxTarEntry(TarEntryType.RegularFile, rel)
                    {
                        DataStream = File.OpenRead(f)
                    };
                    await tar.WriteEntryAsync(entry, token);
                    entry.DataStream?.Dispose();
                }
            }

            var digest = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            var tag = $"gzctf-auto/{req.GameId}/{slug}:{digest[..12]}";

            // Fast path: if the tag already exists locally, treat as a
            // no-op so admin re-imports / re-scans don't waste cycles.
            try
            {
                var existing = await _client.Images.InspectImageAsync(tag, token);
                logger.LogInformation("BuildAsync: image {Tag} already exists locally (digest {Id})", tag, existing.ID);
                return new ChallengeBuildResult(true, tag, existing.ID, "(cached)", null);
            }
            catch (DockerImageNotFoundException) { /* fall through */ }
            catch (DockerApiException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) { /* fall through */ }

            using var timeout = new CancellationTokenSource(BuildTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);

            var logTail = new StringBuilder();
            string? lastError = null;

            var progress = new Progress<JSONMessage>(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Stream))
                    AppendTail(logTail, msg.Stream);
                if (!string.IsNullOrEmpty(msg.Status))
                    AppendTail(logTail, msg.Status + "\n");
                if (msg.Error is { Message: { Length: > 0 } em })
                    lastError = em;
            });

            await using (var contextStream = File.OpenRead(contextTar))
            {
                await _client.Images.BuildImageFromDockerfileAsync(
                    new ImageBuildParameters
                    {
                        Dockerfile = req.Dockerfile,
                        Tags = [tag],
                        Remove = true,
                        ForceRemove = true,
                        NoCache = false,
                    },
                    contextStream,
                    authConfigs: null,
                    headers: null,
                    progress: progress,
                    linked.Token);
            }

            if (lastError is not null)
            {
                logger.LogWarning("BuildAsync: build failed for {Tag}: {Err}", tag, lastError);
                return new ChallengeBuildResult(false, null, null, logTail.ToString(), lastError);
            }

            // Confirm the image actually exists and grab a digest.
            string? imageId = null;
            try
            {
                var inspect = await _client.Images.InspectImageAsync(tag, token);
                imageId = inspect.ID;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "BuildAsync: image {Tag} inspect failed after build", tag);
            }

            return new ChallengeBuildResult(true, tag, imageId, logTail.ToString(), null);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return new ChallengeBuildResult(false, null, null, "(cancelled)", "Build cancelled by host.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "DockerChallengeImageBuilder: build failed");
            return new ChallengeBuildResult(false, null, null, e.Message, e.Message);
        }
        finally
        {
            try { File.Delete(contextTar); } catch { /* best effort */ }
        }
    }

    static void AppendTail(StringBuilder sb, string line)
    {
        sb.Append(line);
        if (sb.Length > LogTailBytes)
            sb.Remove(0, sb.Length - LogTailBytes);
    }

    static string NormalizeSlug(string s)
    {
        var clean = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant())
            clean.Append(char.IsLetterOrDigit(c) ? c : '-');
        var slug = clean.ToString().Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Length > 0 ? slug : "challenge";
    }
}
