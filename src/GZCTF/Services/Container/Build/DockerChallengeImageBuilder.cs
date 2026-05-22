using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using Microsoft.Extensions.Options;

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
    IOptionsMonitor<BuildRegistryConfig> registryConfig,
    IConfiguration configuration,
    ILogger<DockerChallengeImageBuilder> logger) : IChallengeImageBuilder
{
    private readonly DockerClient _client = provider.GetProvider();
    private readonly byte[] _xorKey = configuration["XorKey"]?.ToUTF8Bytes() ?? [];
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PushTimeout = TimeSpan.FromMinutes(10);
    private const int LogTailBytes = 32 * 1024;

    public async Task<ChallengeBuildResult> BuildAsync(
        ChallengeBuildRequest req,
        CancellationToken token,
        Action<string>? onProgress = null)
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
                {
                    AppendTail(logTail, msg.Stream);
                    try { onProgress?.Invoke(msg.Stream); } catch { /* sink errors must not break the build */ }
                }
                if (!string.IsNullOrEmpty(msg.Status))
                {
                    var line = msg.Status + "\n";
                    AppendTail(logTail, line);
                    try { onProgress?.Invoke(line); } catch { /* sink errors must not break the build */ }
                }
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

            // Optional registry push. The local tag is what's stored
            // on the daemon; if the operator configured a push target
            // (BuildRegistryConfig.PushOnBuild), we retag with the
            // registry prefix and push. The returned ImageTag is the
            // registry tag — that's what the challenge row's
            // ContainerImage gets set to, and the runner pulls it from
            // there.
            var reg = registryConfig.CurrentValue;
            if (reg.IsConfigured)
            {
                var pushed = await TryPushAsync(tag, req.GameId, slug, digest[..12],
                    reg, logTail, linked.Token);
                if (pushed is not null)
                    return new ChallengeBuildResult(true, pushed, imageId, logTail.ToString(), null);
                // TryPushAsync wrote the failure into logTail before
                // returning null; surface it as a build failure so the
                // operator sees what went wrong.
                return new ChallengeBuildResult(false, null, null, logTail.ToString(),
                    "Registry push failed — see build log for details.");
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

    /// <summary>
    /// Retag the local image with the registry prefix and push.
    /// Returns the registry tag on success, or null on failure (with
    /// the failure reason already appended to <paramref name="logTail"/>).
    /// </summary>
    /// <param name="localTag">The just-built local tag, e.g.
    /// <c>gzctf-auto/8/tower-of-babel:abc123def456</c>.</param>
    /// <param name="gameId">Owning game id (for tag composition).</param>
    /// <param name="slug">Normalized challenge slug.</param>
    /// <param name="digest">Short content SHA (12 hex chars).</param>
    /// <param name="reg">Live registry config — already validated via
    /// <see cref="BuildRegistryConfig.IsConfigured"/> by the caller.</param>
    private async Task<string?> TryPushAsync(
        string localTag, int gameId, string slug, string digest,
        BuildRegistryConfig reg, StringBuilder logTail, CancellationToken token)
    {
        // Compose the registry tag. Examples:
        //   ghcr.io/myorg/gzctf-auto/8/tower-of-babel:abc123def456
        //   registry.local:5000/gzctf-auto/8/tower-of-babel:abc123def456
        var server = reg.Server!.Trim().TrimEnd('/');
        var ns = string.IsNullOrWhiteSpace(reg.Namespace) ? null : reg.Namespace.Trim().Trim('/');
        var path = ns is null
            ? $"gzctf-auto/{gameId}/{slug}"
            : $"{ns}/gzctf-auto/{gameId}/{slug}";
        var repository = $"{server}/{path}";
        var registryTag = $"{repository}:{digest}";

        AppendTail(logTail, $"\n[push] retagging {localTag} → {registryTag}\n");

        try
        {
            await _client.Images.TagImageAsync(localTag,
                new ImageTagParameters { RepositoryName = repository, Tag = digest },
                token);
        }
        catch (Exception ex)
        {
            AppendTail(logTail, $"[push] tag failed: {ex.Message}\n");
            logger.LogWarning(ex, "Registry push: tag failed {From} → {To}", localTag, registryTag);
            return null;
        }

        AuthConfig? auth = null;
        if (!string.IsNullOrEmpty(reg.Username))
        {
            auth = new AuthConfig
            {
                ServerAddress = server,
                Username = reg.Username,
                Password = DecryptPassword(reg.Password),
            };
        }

        AppendTail(logTail, $"[push] pushing to {server}…\n");
        using var pushTimeout = new CancellationTokenSource(PushTimeout);
        using var linkedPush = CancellationTokenSource.CreateLinkedTokenSource(token, pushTimeout.Token);

        try
        {
            var pushProgress = new Progress<JSONMessage>(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Status))
                    AppendTail(logTail, $"[push] {msg.Status}{(string.IsNullOrEmpty(msg.Progress?.Current.ToString()) ? "" : " " + msg.Progress?.Current)}\n");
                if (msg.Error is { Message: { Length: > 0 } em })
                    AppendTail(logTail, $"[push] error: {em}\n");
            });
            await _client.Images.PushImageAsync(
                repository,
                new ImagePushParameters { Tag = digest },
                auth,
                pushProgress,
                linkedPush.Token);
        }
        catch (OperationCanceledException) when (pushTimeout.IsCancellationRequested)
        {
            AppendTail(logTail, $"[push] timed out after {PushTimeout.TotalMinutes:0}m\n");
            return null;
        }
        catch (Exception ex)
        {
            AppendTail(logTail, $"[push] failed: {ex.Message}\n");
            logger.LogWarning(ex, "Registry push: push failed {Tag}", registryTag);
            return null;
        }

        AppendTail(logTail, $"[push] OK — image available at {registryTag}\n");
        return registryTag;
    }

    /// <summary>
    /// Reverse the XOR obfuscation applied at config-save time. If the
    /// XorKey is empty (test setups) or the stored value isn't valid
    /// base64, fall through to returning the raw stored value — that's
    /// the same defensive behavior the existing key-pair handling uses.
    /// </summary>
    private string DecryptPassword(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return string.Empty;
        if (_xorKey.Length == 0) return stored;
        try
        {
            return System.Text.Encoding.UTF8.GetString(
                Codec.Xor(Convert.FromBase64String(stored), _xorKey));
        }
        catch
        {
            // Pre-encryption legacy value or malformed input — try the
            // stored value as-is so a misconfigured XorKey doesn't
            // permanently break pushes.
            return stored;
        }
    }

    /// <summary>
    /// Token-shape scrubber for build output. A Dockerfile that
    /// <c>echo</c>s a GitHub PAT (legitimately during a `gh auth status`
    /// or buggily during a leaked $GITHUB_TOKEN) would otherwise land
    /// the raw token in <c>Challenge.LastBuildLog</c> and
    /// <c>ChallengeBuildAudit.LogTail</c>, both of which are visible
    /// to anyone with admin access. Replace before append.
    ///
    /// <para>The patterns are GitHub's well-known PAT prefixes plus
    /// the AWS access-key shape. False positives are acceptable — a
    /// scrubbed log is more useful than a leaked secret.</para>
    /// </summary>
    private static readonly Regex[] SecretPatterns =
    [
        new Regex(@"ghp_[A-Za-z0-9]{36}", RegexOptions.Compiled),
        new Regex(@"github_pat_[A-Za-z0-9_]{82,}", RegexOptions.Compiled),
        new Regex(@"gho_[A-Za-z0-9]{36}", RegexOptions.Compiled),
        new Regex(@"ghs_[A-Za-z0-9]{36}", RegexOptions.Compiled),
        new Regex(@"ghr_[A-Za-z0-9]{36}", RegexOptions.Compiled),
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled),
    ];

    internal static string ScrubSecrets(string line)
    {
        foreach (var p in SecretPatterns)
            line = p.Replace(line, "***SCRUBBED***");
        return line;
    }

    static void AppendTail(StringBuilder sb, string line)
    {
        line = ScrubSecrets(line);
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
