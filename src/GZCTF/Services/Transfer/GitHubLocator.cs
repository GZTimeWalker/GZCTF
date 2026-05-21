using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Parses public github.com URLs into <c>(owner, repo, ref, subpath)</c>
/// and wraps the two GitHub REST calls the importer needs: resolving the
/// HEAD commit SHA and downloading a repo tarball.
///
/// Public repos only in v1 — no token, no enterprise hosts. URLs that
/// don't match the github.com pattern are rejected at parse time so we
/// never make a request anywhere else.
/// </summary>
public sealed partial class GitHubLocator
{
    [GeneratedRegex(@"^https?://github\.com/(?<owner>[A-Za-z0-9._-]+)/(?<repo>[A-Za-z0-9._-]+?)(?:\.git)?(?:/tree/(?<ref>[^/]+)(?:/(?<subpath>.+))?)?/?$",
        RegexOptions.Compiled)]
    private static partial Regex GitHubUrlRegex();

    public string Owner { get; }
    public string Repo { get; }
    public string? Ref { get; }
    public string? Subpath { get; }
    public string OriginalUrl { get; }

    private GitHubLocator(string owner, string repo, string? @ref, string? subpath, string originalUrl)
    {
        Owner = owner;
        Repo = repo;
        Ref = @ref;
        Subpath = subpath;
        OriginalUrl = originalUrl;
    }

    public static bool TryParse(string url, string? overrideRef, string? overrideSubpath, out GitHubLocator? loc, out string? error)
    {
        loc = null;
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL is required.";
            return false;
        }

        var m = GitHubUrlRegex().Match(url.Trim());
        if (!m.Success)
        {
            error = "Only public github.com URLs are supported in v1 (https://github.com/{owner}/{repo}[/tree/{ref}/{subpath}]).";
            return false;
        }

        var owner = m.Groups["owner"].Value;
        var repo = m.Groups["repo"].Value;
        var parsedRef = m.Groups["ref"].Success ? m.Groups["ref"].Value : null;
        var parsedSubpath = m.Groups["subpath"].Success ? m.Groups["subpath"].Value : null;

        // Explicit overrides win over URL-embedded values.
        var finalRef = !string.IsNullOrWhiteSpace(overrideRef) ? overrideRef.Trim() : parsedRef;
        var finalSubpath = !string.IsNullOrWhiteSpace(overrideSubpath) ? overrideSubpath.Trim().TrimEnd('/') : parsedSubpath?.TrimEnd('/');

        loc = new GitHubLocator(owner, repo, finalRef, finalSubpath, url.Trim());
        return true;
    }

    /// <summary>
    /// Resolves the commit SHA for <see cref="Ref"/> (or the repo's default
    /// branch when <see cref="Ref"/> is null) via the GitHub REST API.
    /// Returns null on any failure — caller logs.
    /// </summary>
    public async Task<string?> GetHeadShaAsync(HttpClient http, string? authToken, CancellationToken token)
    {
        var refPart = string.IsNullOrEmpty(Ref) ? "HEAD" : Ref;
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{Uri.EscapeDataString(refPart)}";
        using var req = BuildRequest(HttpMethod.Get, url, authToken);

        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
        if (!resp.IsSuccessStatusCode) return null;

        await using var body = await resp.Content.ReadAsStreamAsync(token);
        using var doc = await JsonDocument.ParseAsync(body, cancellationToken: token);
        return doc.RootElement.TryGetProperty("sha", out var sha) ? sha.GetString() : null;
    }

    /// <summary>
    /// Downloads the repo as a tarball (gzipped tar) from the GitHub
    /// codeload endpoint. Caller owns the returned stream.
    /// </summary>
    public async Task<Stream> DownloadTarballAsync(HttpClient http, string? authToken, CancellationToken token)
    {
        var refPart = string.IsNullOrEmpty(Ref) ? "HEAD" : Ref;
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/tarball/{Uri.EscapeDataString(refPart)}";
        using var req = BuildRequest(HttpMethod.Get, url, authToken);

        var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(token);
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string? authToken)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(authToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        return req;
    }
}
