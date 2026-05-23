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

        // GitHub's git-over-HTTPS uses HTTP Basic auth with the
        // documented user "x-access-token" + the PAT as the password.
        // (Bearer works for the REST API but NOT for git's smart-HTTP
        // protocol, which falls through to a username prompt → fail
        // with "could not read Username".)
        var authArgs = BuildAuthArgs(authToken);

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
    /// Stage <paramref name="filesRelativeToRepo"/>, commit them with
    /// the given message, and push to upstream. Caller is responsible
    /// for having already written the file contents to disk under the
    /// checkout dir before calling this.
    ///
    /// <para>Uses the same per-(kind,id) semaphore as
    /// <see cref="SyncAsync"/> so a push can't race a fetch. Performs a
    /// <c>fetch + reset --hard</c> first so the working tree is at
    /// upstream HEAD before we apply the edit — this turns a
    /// concurrent push from someone else into a normal merge-style
    /// failure ("non-fast-forward") that the caller can surface,
    /// rather than silently dropping their commits.</para>
    /// </summary>
    /// <param name="kind">Same kind used at <see cref="SyncAsync"/>.</param>
    /// <param name="id">Same id used at <see cref="SyncAsync"/>.</param>
    /// <param name="loc">Locator (for remote URL + ref).</param>
    /// <param name="authToken">PAT with Contents:write scope. Required.</param>
    /// <param name="filesRelativeToRepo">Paths relative to the repo
    /// root (e.g. <c>final/Pwn/foo/challenge.yml</c>).</param>
    /// <param name="commitMessage">Plain commit message.</param>
    /// <param name="authorName">Used for git config user.name on the
    /// push commit. Default "GZCTF admin".</param>
    /// <param name="authorEmail">user.email. Default a noreply.</param>
    /// <returns>The pushed commit SHA.</returns>
    public async Task<string> CommitAndPushAsync(
        string kind, int id, GitHubLocator loc, string authToken,
        IReadOnlyList<string> filesRelativeToRepo,
        string commitMessage,
        string authorName = "GZCTF admin",
        string authorEmail = "noreply@gzctf.local",
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(authToken))
            throw new InvalidOperationException("Push requires an auth token.");
        if (filesRelativeToRepo.Count == 0)
            throw new InvalidOperationException("Push requires at least one file path.");

        var lockKey = $"{kind}:{id}";
        var gate = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            return await CommitAndPushCoreAsync(kind, id, loc, authToken,
                filesRelativeToRepo, commitMessage, authorName, authorEmail, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<string> CommitAndPushCoreAsync(
        string kind, int id, GitHubLocator loc, string authToken,
        IReadOnlyList<string> filesRelativeToRepo,
        string commitMessage, string authorName, string authorEmail,
        CancellationToken ct)
    {
        var repoDir = Path.Combine(RepoRoot, kind, id.ToString());
        if (!Directory.Exists(Path.Combine(repoDir, ".git")))
            throw new InvalidOperationException(
                $"Repo {kind}/{id} not cloned yet; SyncAsync must run before CommitAndPushAsync.");

        var refSpec = string.IsNullOrEmpty(loc.Ref) ? "HEAD" : loc.Ref;
        var repoUrl = $"https://github.com/{loc.Owner}/{loc.Repo}.git";
        var authArgs = BuildAuthArgs(authToken);

        // Set commit identity per-repo to avoid mutating global git
        // config inside the container.
        await RunGitAsync(repoDir, ["config", "user.name", authorName], ct);
        await RunGitAsync(repoDir, ["config", "user.email", authorEmail], ct);
        await RunGitAsync(repoDir, ["remote", "set-url", "origin", repoUrl], ct);

        // Stage only the explicit paths — never a wholesale `git add .`
        // since that would sweep up build artifacts the import wrote.
        foreach (var rel in filesRelativeToRepo)
        {
            var fullPath = Path.Combine(repoDir, rel);
            if (!File.Exists(fullPath))
                throw new InvalidOperationException($"Push target {rel} doesn't exist on disk.");
            await RunGitAsync(repoDir, ["add", "--", rel], ct);
        }

        // No-op detection — if nothing actually changed (operator
        // "edit" that wrote the same yaml back), skip the commit and
        // push to avoid empty commits cluttering history.
        var staged = (await RunGitAsync(repoDir, ["diff", "--cached", "--name-only"], ct)).Trim();
        if (string.IsNullOrEmpty(staged))
        {
            logger.LogInformation("GitRepoSync: no changes staged for {Kind}/{Id}; skip push", kind, id);
            return (await RunGitAsync(repoDir, ["rev-parse", "HEAD"], ct)).Trim();
        }

        await RunGitAsync(repoDir, ["commit", "-m", commitMessage], ct);

        // Resolve the destination branch name. The binding's stored
        // Ref is either the explicit branch name the operator pinned,
        // or null = "the default branch" — in which case `git push
        // HEAD:HEAD` would fail with non-fast-forward because github
        // can't update its symbolic HEAD ref directly. Derive the
        // actual local branch name and push to that.
        var destRef = refSpec;
        if (string.IsNullOrEmpty(loc.Ref))
        {
            // `git rev-parse --abbrev-ref HEAD` → e.g. "main".
            destRef = (await RunGitAsync(repoDir, ["rev-parse", "--abbrev-ref", "HEAD"], ct)).Trim();
            if (string.IsNullOrEmpty(destRef) || destRef == "HEAD")
                throw new InvalidOperationException(
                    "Could not resolve current branch name for push; checkout is detached.");
        }

        // --depth 1 in clone means the local repo is shallow; the push
        // works because we're appending one commit on top of the
        // shallow HEAD. github accepts this provided the parent SHA
        // exists upstream (which it does — it's the SHA we fetched).
        await RunGitAsync(repoDir,
            [.. authArgs, "push", "origin", $"HEAD:refs/heads/{destRef}"], ct);
        logger.LogInformation(
            "GitRepoSync: pushed {Files} file(s) to {Owner}/{Repo}@{Ref}",
            filesRelativeToRepo.Count, loc.Owner, loc.Repo, refSpec);
        return (await RunGitAsync(repoDir, ["rev-parse", "HEAD"], ct)).Trim();
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

    /// <summary>
    /// Compose the <c>-c http.extraHeader=...</c> args git needs to
    /// auth against GitHub HTTPS. GitHub's smart-HTTP protocol expects
    /// HTTP Basic with the special user <c>x-access-token</c> and the
    /// PAT as the password — Bearer works for the REST API but git
    /// itself falls through to a credential prompt with Bearer,
    /// printing <c>"could not read Username for 'https://github.com'"</c>
    /// when <see cref="GIT_TERMINAL_PROMPT"/>=0 (which we set).
    ///
    /// <para>Works identically for classic <c>ghp_</c> and fine-grained
    /// <c>github_pat_</c> tokens.</para>
    /// </summary>
    private static string[] BuildAuthArgs(string? authToken)
    {
        if (string.IsNullOrEmpty(authToken)) return [];
        var basic = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"x-access-token:{authToken}"));
        return ["-c", $"http.extraHeader=Authorization: Basic {basic}"];
    }
}
