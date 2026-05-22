using System.Collections.Concurrent;
using System.Diagnostics;

namespace GZCTF.Services.Transfer;

/// <summary>
/// One synced checkout of a github repo on local disk. Lives under
/// <see cref="GitRepoSyncService.RepoRoot"/> and persists across scans,
/// so the second-and-later <c>git fetch</c> is a small delta instead of
/// a multi-MB tarball.
/// </summary>
public sealed record RepoSnapshot(string CheckoutPath, string CommitSha);

/// <summary>
/// Manages persistent shallow git clones keyed by (kind, id). Replaces
/// the prior tarball-download flow which redownloaded the entire repo
/// every poll tick — for a 2-event repo like findit-ctf-2026 that was
/// three full downloads per scan, totalling 30+ seconds; the git path
/// is a single ~150KB fetch on the steady-state path.
///
/// <para><b>State on disk:</b> <c>/app/repos/{kind}/{id}/.git</c>. The
/// directory is created on first sync and reused thereafter. If the
/// container's volume is wiped, the next sync re-clones automatically.</para>
///
/// <para><b>Auth:</b> tokens are passed via <c>git -c
/// http.extraHeader="Authorization: Bearer ..."</c> for the duration of
/// a single invocation. They are NOT written to <c>.git/config</c> — a
/// later <c>cat .git/config</c> on the checkout shows only the public
/// repo URL, never the PAT.</para>
///
/// <para><b>Threading:</b> concurrent syncs of the SAME <c>(kind,id)</c>
/// (e.g. background poller tick coinciding with an admin "Scan now",
/// or with the per-challenge Build fallback) are serialized through a
/// per-key <see cref="SemaphoreSlim"/>. Without this, two
/// <c>git fetch</c>s racing on the same working tree can leave the
/// checkout at a half-applied SHA. Different keys still run in
/// parallel — the lock is fine-grained.</para>
///
/// <para>The lock is in-process only. GZCTF is single-process today;
/// if/when multi-instance ships, switch to
/// <c>pg_advisory_xact_lock(hash('binding:42'))</c> — see
/// <c>GameInstanceRepository.cs:326-333</c> for an existing example.</para>
/// </summary>
public sealed class GitRepoSyncService(ILogger<GitRepoSyncService> logger)
{
    /// <summary>
    /// Volume-mounted location (<c>gzctf-repos</c> in compose). Kept
    /// outside /app/files because the repo-cache lifecycle is distinct
    /// from user-uploaded blobs (we can rebuild it from upstream).
    /// </summary>
    public const string RepoRoot = "/app/repos";

    /// <summary>
    /// Hard wall-clock cap on a single git invocation. Two minutes is
    /// generous for a shallow clone of any real-world CTF repo — past
    /// that we assume the connection is wedged (DNS, TLS handshake,
    /// proxy, revoked token mid-fetch). The process is killed and the
    /// caller sees <see cref="OperationCanceledException"/>.
    /// </summary>
    private static readonly TimeSpan GitCommandTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Per-(kind,id) mutex. Allocated lazily on first sync and kept for
    /// the process lifetime — one entry per binding/watch is negligible
    /// memory and skipping the dictionary churn keeps the hot path
    /// allocation-free.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Shallow-clone (or fast-forward fetch) the repo identified by
    /// <paramref name="loc"/> and check out <see cref="GitHubLocator.Ref"/>
    /// (or the upstream default branch if Ref is null).
    /// </summary>
    /// <param name="kind">Namespace under <see cref="RepoRoot"/>, e.g.
    /// <c>"binding"</c> or <c>"watch"</c>. Keeps binding and watch
    /// clones from colliding even when both point at the same repo.</param>
    /// <param name="id">Numeric id of the binding/watch. Becomes the
    /// directory name.</param>
    public async Task<RepoSnapshot> SyncAsync(
        string kind, int id, GitHubLocator loc, string? authToken, CancellationToken ct)
    {
        var lockKey = $"{kind}:{id}";
        var gate = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            return await SyncCoreAsync(kind, id, loc, authToken, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<RepoSnapshot> SyncCoreAsync(
        string kind, int id, GitHubLocator loc, string? authToken, CancellationToken ct)
    {
        var kindDir = Path.Combine(RepoRoot, kind);
        Directory.CreateDirectory(kindDir);
        var repoDir = Path.Combine(kindDir, id.ToString());
        var gitDir = Path.Combine(repoDir, ".git");

        var repoUrl = $"https://github.com/{loc.Owner}/{loc.Repo}.git";
        var refSpec = string.IsNullOrEmpty(loc.Ref) ? "HEAD" : loc.Ref;

        var authArgs = !string.IsNullOrEmpty(authToken)
            ? new[] { "-c", $"http.extraHeader=Authorization: Bearer {authToken}" }
            : Array.Empty<string>();

        if (!Directory.Exists(gitDir))
        {
            // Fresh clone. --depth 1 because we only ever look at HEAD;
            // we never inspect history. --single-branch keeps the fetch
            // surface small on repos with hundreds of feature branches.
            logger.LogInformation("GitRepoSync: cloning {Url} → {Dir}", repoUrl, repoDir);
            await RunGitAsync(kindDir, [
                .. authArgs,
                "clone", "--depth", "1", "--single-branch",
                .. (string.IsNullOrEmpty(loc.Ref) ? Array.Empty<string>() : new[] { "--branch", loc.Ref }),
                repoUrl, id.ToString()
            ], ct);
        }
        else
        {
            // Incremental: fetch the requested ref, then point the
            // worktree at it. --depth 1 again to avoid pulling history
            // that accumulates over months of poll ticks.
            logger.LogDebug("GitRepoSync: fetching {Url} in {Dir}", repoUrl, repoDir);
            // Make sure the remote URL is current (admin may have
            // edited it via the Update endpoint).
            await RunGitAsync(repoDir, ["remote", "set-url", "origin", repoUrl], ct);
            await RunGitAsync(repoDir, [
                .. authArgs,
                "fetch", "--depth", "1", "origin", refSpec
            ], ct);
            // FETCH_HEAD always points to whatever we just fetched.
            await RunGitAsync(repoDir, ["reset", "--hard", "FETCH_HEAD"], ct);
            // Remove untracked files that may have been written by a
            // previous import (e.g. stray __pycache__). -fdx is safe
            // here because the repo dir is internal — nothing should
            // touch it between syncs.
            await RunGitAsync(repoDir, ["clean", "-fdx"], ct);
        }

        var sha = (await RunGitAsync(repoDir, ["rev-parse", "HEAD"], ct)).Trim();
        return new RepoSnapshot(repoDir, sha);
    }

    /// <summary>
    /// Drop the on-disk checkout. Called on binding/watch deletion so
    /// the cache doesn't grow indefinitely.
    /// </summary>
    public void DropCache(string kind, int id)
    {
        var repoDir = Path.Combine(RepoRoot, kind, id.ToString());
        try
        {
            if (Directory.Exists(repoDir))
            {
                Directory.Delete(repoDir, recursive: true);
                logger.LogInformation("GitRepoSync: dropped {Dir}", repoDir);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GitRepoSync: failed to drop {Dir}", repoDir);
        }
    }

    /// <summary>
    /// Shell out to the system <c>git</c> binary. Stdout is captured
    /// and returned; stderr is captured for error messages but never
    /// emitted unless the exit code is non-zero. A non-zero exit
    /// throws so callers can let the scan's existing try/catch report
    /// it to the audit row.
    /// </summary>
    private static async Task<string> RunGitAsync(string cwd, string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        // Block interactive credential prompts so a misconfigured
        // private repo fails fast instead of hanging the worker.
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("git failed to start");

        // Hard wall-clock cap on the git call. The original token
        // would have allowed an indefinite hang on a wedged network
        // connection — this keeps the worker thread alive for
        // subsequent bindings instead of starving the scan tick.
        using var timeout = new CancellationTokenSource(GitCommandTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(linked.Token);
        var stderrTask = proc.StandardError.ReadToEndAsync(linked.Token);
        try
        {
            await proc.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            // Kill the wedged git process so it doesn't linger past the
            // worker. The kill is best-effort: if the process already
            // exited we ignore the InvalidOperationException.
            try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new OperationCanceledException(
                $"git {SafeCommandSummary(args)} timed out after {GitCommandTimeout.TotalSeconds:0}s");
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (proc.ExitCode != 0)
        {
            // Strip any echoed Authorization header from stderr just in
            // case git ever prints it (unlikely, but cheap insurance).
            var sanitized = stderr.Replace("Authorization: Bearer ", "Authorization: Bearer ***");
            throw new InvalidOperationException(
                $"git {SafeCommandSummary(args)} exited {proc.ExitCode}: {sanitized.Trim()}");
        }

        return stdout;
    }

    /// <summary>
    /// Build a human-readable summary of the git command for error
    /// messages, stopping at the first <c>-c</c> so the
    /// <c>http.extraHeader=Authorization: Bearer ...</c> token never
    /// lands in an exception message.
    /// </summary>
    private static string SafeCommandSummary(string[] args)
        => string.Join(' ', args.TakeWhile(a => a != "-c"));
}
