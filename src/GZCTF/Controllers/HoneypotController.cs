using GZCTF.Models.Internal;
using GZCTF.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

/// <summary>
/// Platform-wide HTTP bait routes. Any hit indicates automated reconnaissance.
/// Each response cross-references sibling bait paths and the configured port honeypots,
/// forming a graph: a link-following scanner or AI agent that chases discovered URLs
/// will trip multiple distinct baits, which the chain detector escalates to a
/// HoneypotChain HardSignal.
///
/// Responses look plausible long enough that the scanner records the hit
/// without immediately learning it tripped a tripwire.
/// </summary>
[ApiController]
[AllowAnonymous]
public class HoneypotController(IHoneypotService honeypotService) : ControllerBase
{
    private const string Category = "http";

    // ─────────────────────── Source-control leakage bait ───────────────────────

    [HttpGet(HoneypotBait.GitConfig)]
    public Task<IActionResult> GitConfig(CancellationToken token) =>
        Hit(HoneypotBait.GitConfig, "text/plain; charset=utf-8",
            "[core]\n" +
            "\trepositoryformatversion = 0\n" +
            "\tfilemode = true\n" +
            "\tbare = false\n" +
            "\tlogallrefupdates = true\n" +
            "[remote \"origin\"]\n" +
            "\turl = ssh://git@backup.internal:2222/internal-tools/web.git\n" +
            "\tfetch = +refs/heads/*:refs/remotes/origin/*\n" +
            "[remote \"deploy\"]\n" +
            "\turl = ssh://deploy@backup.internal:2222/srv/web.git\n" +
            "[branch \"main\"]\n" +
            "\tremote = origin\n" +
            "\tmerge = refs/heads/main\n" +
            "[branch \"dev-secrets-2024\"]\n" +
            "\tremote = origin\n" +
            "\tmerge = refs/heads/dev-secrets-2024\n" +
            "[secrets]\n" +
            "\tenvFile = " + HoneypotBait.Env + "\n" +
            "\tawsConfig = " + HoneypotBait.AwsCredentials + "\n",
            token);

    [HttpGet(HoneypotBait.GitHead)]
    public Task<IActionResult> GitHead(CancellationToken token) =>
        Hit(HoneypotBait.GitHead, "text/plain; charset=utf-8", "ref: refs/heads/main\n", token);

    [HttpGet(HoneypotBait.SvnWcDb)]
    public Task<IActionResult> SvnDb(CancellationToken token) =>
        HitNotFound(HoneypotBait.SvnWcDb, token);

    [HttpGet(HoneypotBait.DsStore)]
    public Task<IActionResult> DsStore(CancellationToken token) =>
        HitNotFound(HoneypotBait.DsStore, token);

    // ─────────────────────── Credentials / config bait ─────────────────────────

    [HttpGet(HoneypotBait.Env)]
    public Task<IActionResult> DotEnv(CancellationToken token) =>
        Hit(HoneypotBait.Env, "text/plain; charset=utf-8",
            "APP_NAME=Platform\n" +
            "APP_ENV=production\n" +
            "APP_DEBUG=false\n" +
            "APP_URL=http://localhost\n" +
            "LOG_CHANNEL=stack\n" +
            "\n" +
            "DB_CONNECTION=mysql\n" +
            "DB_HOST=127.0.0.1\n" +
            "DB_PORT=3306\n" +
            "DB_DATABASE=app\n" +
            "DB_USERNAME=app\n" +
            "DB_PASSWORD=changeme\n" +
            "\n" +
            "REDIS_HOST=127.0.0.1\n" +
            "REDIS_PORT=6379\n" +
            "\n" +
            "MAIL_MAILER=smtp\n" +
            "\n" +
            "ADMIN_PORTAL_URL=" + HoneypotBait.AdminPortalLogin + "\n" +
            "INTERNAL_API=" + HoneypotBait.ApiInternalUsers + "\n" +
            "BACKUP_URL=" + HoneypotBait.DbDumpSql + "\n" +
            "SPRING_ACTUATOR=" + HoneypotBait.ActuatorEnv + "\n",
            token);

    [HttpGet(HoneypotBait.AwsCredentials)]
    public Task<IActionResult> AwsCredentials(CancellationToken token) =>
        Hit(HoneypotBait.AwsCredentials, "text/plain; charset=utf-8",
            "[default]\n" +
            "aws_access_key_id = AKIAEXAMPLEKEYNOTREAL\n" +
            "aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY\n" +
            "region = us-east-1\n" +
            "output = json\n" +
            "\n" +
            "[staging]\n" +
            "aws_access_key_id = AKIAEXAMPLESTAGINGKEY\n" +
            "aws_secret_access_key = somelongstringofgibberishforstaging\n" +
            "region = us-west-2\n" +
            "output = json\n" +
            "; bucket internal-staging-backups mirrors " + HoneypotBait.DbDumpSql + " daily\n" +
            "; alt console at " + HoneypotBait.AdminPortalLogin + "\n",
            token);

    // ─────────────────────── CMS / DB admin bait ───────────────────────────────

    [HttpGet(HoneypotBait.WpAdmin)]
    [HttpGet(HoneypotBait.WpAdminSlash)]
    public Task<IActionResult> WpAdmin(CancellationToken token) =>
        HitRedirect(HoneypotBait.WpAdmin, HoneypotBait.WpLogin + "?redirect_to=%2Fwp-admin%2F&reauth=1", token);

    [HttpGet(HoneypotBait.WpLogin)]
    [HttpPost(HoneypotBait.WpLogin)]
    public Task<IActionResult> WpLogin(CancellationToken token) =>
        Hit(HoneypotBait.WpLogin, "text/html; charset=utf-8",
            "<!DOCTYPE html>\n" +
            "<html><head><title>Log In &lsaquo; Platform</title>\n" +
            "<!-- migration in progress: legacy login at " + HoneypotBait.AdminPortalLogin + " -->\n" +
            "<!-- last backup: " + HoneypotBait.WpBackupZip + " -->\n" +
            "</head><body>\n" +
            "<form id=\"loginform\" action=\"" + HoneypotBait.WpLogin + "\" method=\"post\">\n" +
            "<label>Username <input type=\"text\" name=\"log\" /></label>\n" +
            "<label>Password <input type=\"password\" name=\"pwd\" /></label>\n" +
            "<input type=\"submit\" name=\"wp-submit\" value=\"Log In\" />\n" +
            "</form>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.PhpMyAdmin)]
    [HttpGet(HoneypotBait.PhpMyAdminSlash)]
    [HttpGet(HoneypotBait.PhpMyAdminIndex)]
    public Task<IActionResult> PhpMyAdmin(CancellationToken token) =>
        Hit(HoneypotBait.PhpMyAdminSlash, "text/html; charset=utf-8",
            "<!DOCTYPE html>\n" +
            "<html><head><title>phpMyAdmin</title>\n" +
            "<!-- See also: " + HoneypotBait.DbExportPhp + " for direct dump -->\n" +
            "</head><body>\n" +
            "<form method=\"post\" action=\"" + HoneypotBait.PhpMyAdminIndex + "\">\n" +
            "<input name=\"pma_username\" placeholder=\"Username\" />\n" +
            "<input type=\"password\" name=\"pma_password\" placeholder=\"Password\" />\n" +
            "<input type=\"submit\" value=\"Go\" />\n" +
            "</form>\n" +
            "<p><small>Admin tools: <a href=\"" + HoneypotBait.DbExportPhp + "\">db-export</a></small></p>\n" +
            "</body></html>",
            token);

    // ─────────────────────── Server / framework debug bait ─────────────────────

    [HttpGet(HoneypotBait.ServerStatus)]
    public Task<IActionResult> ServerStatus(CancellationToken token) =>
        Hit(HoneypotBait.ServerStatus, "text/html; charset=utf-8",
            "<html><head><title>Apache Status</title></head><body>\n" +
            "<h1>Apache Server Status for localhost</h1>\n" +
            "<dl>\n" +
            "<dt>Server Version: Apache/2.4.41 (Ubuntu)</dt>\n" +
            "<dt>Server Built: 2020-08-12</dt>\n" +
            "<dt>Total accesses: 18472 - Total Traffic: 412.3 MB</dt>\n" +
            "</dl>\n" +
            "<h2>Recent requests</h2>\n" +
            "<pre>\n" +
            "GET " + HoneypotBait.AdminPortalLogin + " HTTP/1.1 200\n" +
            "GET " + HoneypotBait.ApiInternalUsers + " HTTP/1.1 200\n" +
            "GET " + HoneypotBait.InternalDebugConsole + " HTTP/1.1 200\n" +
            "POST " + HoneypotBait.DbExportPhp + " HTTP/1.1 200\n" +
            "</pre></body></html>",
            token);

    [HttpGet(HoneypotBait.Actuator)]
    public Task<IActionResult> Actuator(CancellationToken token) =>
        Hit(HoneypotBait.Actuator, "application/json",
            "{\"_links\":{" +
            "\"self\":{\"href\":\"http://localhost" + HoneypotBait.Actuator + "\"}," +
            "\"env\":{\"href\":\"http://localhost" + HoneypotBait.ActuatorEnv + "\"}," +
            "\"health\":{\"href\":\"http://localhost" + HoneypotBait.ActuatorHealth + "\"}," +
            "\"console\":{\"href\":\"http://localhost" + HoneypotBait.DebugConsole + "\"}" +
            "}}",
            token);

    [HttpGet(HoneypotBait.ActuatorEnv)]
    public Task<IActionResult> ActuatorEnv(CancellationToken token) =>
        Hit(HoneypotBait.ActuatorEnv, "application/json",
            "{\"activeProfiles\":[\"production\"]," +
            "\"propertySources\":[" +
            "{\"name\":\"applicationConfig\",\"properties\":{" +
            "\"spring.datasource.url\":{\"value\":\"jdbc:mysql://127.0.0.1:3306/app\"}," +
            "\"spring.redis.host\":{\"value\":\"127.0.0.1\"}," +
            "\"spring.redis.port\":{\"value\":\"6379\"}," +
            "\"management.endpoints.web.exposure.include\":{\"value\":\"*\"}," +
            "\"debug.console.url\":{\"value\":\"" + HoneypotBait.DebugConsole + "\"}" +
            "}}]}",
            token);

    [HttpGet(HoneypotBait.ActuatorHealth)]
    public Task<IActionResult> ActuatorHealth(CancellationToken token) =>
        Hit(HoneypotBait.ActuatorHealth, "application/json",
            "{\"status\":\"UP\",\"components\":{" +
            "\"db\":{\"status\":\"UP\",\"details\":{\"database\":\"MySQL\",\"validationQuery\":\"isValid()\"}}," +
            "\"ping\":{\"status\":\"UP\"}," +
            "\"actuator\":{\"href\":\"" + HoneypotBait.Actuator + "\"}}}",
            token);

    [HttpGet(HoneypotBait.Ignition)]
    [HttpPost(HoneypotBait.Ignition)]
    public Task<IActionResult> Ignition(CancellationToken token) =>
        Hit(HoneypotBait.Ignition, "application/json",
            "{\"error\":\"unknown solution\",\"hint\":\"see " + HoneypotBait.DebugConsole + " for live debugger\"}",
            token);

    [HttpGet(HoneypotBait.CgiLuci)]
    public Task<IActionResult> CgiBin(CancellationToken token) =>
        Hit(HoneypotBait.CgiLuci, "text/html; charset=utf-8",
            "<html><body><h1>LuCI</h1><p>Migrated to <a href=\"" + HoneypotBait.AdminPortalLogin + "\">admin portal</a>.</p></body></html>",
            token);

    // ─────────────────────── Backup file bait ──────────────────────────────────

    [HttpGet(HoneypotBait.BackupZip)]
    [HttpGet(HoneypotBait.BackupTarGz)]
    [HttpGet(HoneypotBait.DatabaseSql)]
    public Task<IActionResult> Backup(CancellationToken token)
    {
        var path = HttpContext.Request.Path.ToString();
        return HitNotFound(path, token);
    }

    // ─────────────────────── Chain-graph endpoints ─────────────────────────────

    [HttpGet(HoneypotBait.AdminPortalLogin)]
    [HttpPost(HoneypotBait.AdminPortalLogin)]
    public Task<IActionResult> AdminPortalLogin(CancellationToken token) =>
        Hit(HoneypotBait.AdminPortalLogin, "text/html; charset=utf-8",
            "<!DOCTYPE html>\n" +
            "<html><head><title>InternalAdmin v2.4 — Sign in</title></head><body>\n" +
            "<h1>InternalAdmin</h1>\n" +
            "<form method=\"post\" action=\"" + HoneypotBait.AdminPortalLogin + "\">\n" +
            "<input name=\"user\" placeholder=\"Username\" />\n" +
            "<input type=\"password\" name=\"pass\" placeholder=\"Password\" />\n" +
            "<input type=\"submit\" value=\"Sign in\" />\n" +
            "</form>\n" +
            "<footer><small>API: <a href=\"" + HoneypotBait.ApiInternalUsers + "\">user list</a> | " +
            "Console: <a href=\"" + HoneypotBait.AdminPortalDashboard + "\">dashboard</a></small></footer>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.AdminPortalDashboard)]
    public Task<IActionResult> AdminPortalDashboard(CancellationToken token) =>
        Hit(HoneypotBait.AdminPortalDashboard, "text/html; charset=utf-8",
            "<html><body>\n" +
            "<h1>InternalAdmin Dashboard</h1>\n" +
            "<ul>\n" +
            "<li><a href=\"" + HoneypotBait.DbExportPhp + "\">DB Export</a></li>\n" +
            "<li><a href=\"" + HoneypotBait.DbDumpSql + "\">Latest dump</a></li>\n" +
            "<li><a href=\"" + HoneypotBait.ApiInternalUsers + "\">Users JSON</a></li>\n" +
            "</ul>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.ApiInternalUsers)]
    public Task<IActionResult> ApiInternalUsers(CancellationToken token) =>
        Hit(HoneypotBait.ApiInternalUsers, "application/json",
            "{\"users\":[" +
            "{\"id\":1,\"name\":\"admin\",\"role\":\"superuser\"}," +
            "{\"id\":2,\"name\":\"deploy\",\"role\":\"automation\"}," +
            "{\"id\":3,\"name\":\"backup\",\"role\":\"automation\"}" +
            "],\"meta\":{" +
            "\"login_url\":\"" + HoneypotBait.AdminPortalLogin + "\"," +
            "\"debug_url\":\"" + HoneypotBait.InternalDebugConsole + "\"," +
            "\"console_url\":\"" + HoneypotBait.DebugConsole + "\"}}",
            token);

    [HttpGet(HoneypotBait.InternalDebugConsole)]
    public Task<IActionResult> InternalDebugConsole(CancellationToken token) =>
        Hit(HoneypotBait.InternalDebugConsole, "text/html; charset=utf-8",
            "<html><body>\n" +
            "<h1>Internal Debug Console</h1>\n" +
            "<p>Live console: <a href=\"" + HoneypotBait.DebugConsole + "\">/_debug/console</a></p>\n" +
            "<p>Cloud creds: <a href=\"" + HoneypotBait.AwsCredentials + "\">.aws/credentials</a></p>\n" +
            "<p>Spring env: <a href=\"" + HoneypotBait.ActuatorEnv + "\">/actuator/env</a></p>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.DebugConsole)]
    public Task<IActionResult> DebugConsole(CancellationToken token) =>
        Hit(HoneypotBait.DebugConsole, "text/html; charset=utf-8",
            "<html><head><title>Debugger PIN required</title></head><body>\n" +
            "<h1>Werkzeug Debugger</h1>\n" +
            "<p>The debugger caught an exception in your WSGI application.</p>\n" +
            "<ul>\n" +
            "<li>Latest backup: <a href=\"" + HoneypotBait.WpBackupZip + "\">backup-2024-q3.zip</a></li>\n" +
            "<li>DB tools: <a href=\"" + HoneypotBait.DbExportPhp + "\">db-export.php</a></li>\n" +
            "<li>Users: <a href=\"" + HoneypotBait.ApiInternalUsers + "\">/api/internal/users.json</a></li>\n" +
            "</ul>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.WpBackupZip)]
    public Task<IActionResult> WpBackupZip(CancellationToken token) =>
        Hit(HoneypotBait.WpBackupZip, "text/plain; charset=utf-8",
            "# stub backup index — full archive at " + HoneypotBait.DbExportPhp + "\n" +
            "# canonical dump: " + HoneypotBait.DbDumpSql + "\n",
            token);

    [HttpGet(HoneypotBait.DbExportPhp)]
    [HttpPost(HoneypotBait.DbExportPhp)]
    public Task<IActionResult> DbExportPhp(CancellationToken token) =>
        Hit(HoneypotBait.DbExportPhp, "text/html; charset=utf-8",
            "<html><body>\n" +
            "<h1>Database Export</h1>\n" +
            "<p>Last dump available at <a href=\"" + HoneypotBait.DbDumpSql + "\">/backups/db-dump.sql</a></p>\n" +
            "<p>Manage tables in <a href=\"" + HoneypotBait.PhpMyAdminSlash + "\">phpMyAdmin</a></p>\n" +
            "</body></html>",
            token);

    [HttpGet(HoneypotBait.DbDumpSql)]
    public Task<IActionResult> DbDumpSql(CancellationToken token) =>
        Hit(HoneypotBait.DbDumpSql, "application/sql",
            "-- MySQL dump (truncated)\n" +
            "-- Source: see " + HoneypotBait.AdminPortalLogin + " for restore UI\n" +
            "-- User list mirror: " + HoneypotBait.ApiInternalUsers + "\n" +
            "--\n" +
            "CREATE TABLE `users` (\n" +
            "  `id` INT NOT NULL AUTO_INCREMENT,\n" +
            "  `username` VARCHAR(64) NOT NULL,\n" +
            "  `password_hash` VARCHAR(255) NOT NULL,\n" +
            "  `role` VARCHAR(32) NOT NULL,\n" +
            "  PRIMARY KEY (`id`)\n" +
            ");\n" +
            "INSERT INTO `users` VALUES (1,'admin','$2y$10$placeholderhashvaluexx','superuser');\n",
            token);

    // ─────────────────────── Helpers ───────────────────────────────────────────

    private async Task<IActionResult> Hit(string bait, string contentType, string body, CancellationToken token)
    {
        await honeypotService.RecordHit(HttpContext, bait, Category, token: token);
        return Content(body, contentType);
    }

    private async Task<IActionResult> HitNotFound(string bait, CancellationToken token)
    {
        await honeypotService.RecordHit(HttpContext, bait, Category, token: token);
        return NotFound();
    }

    private async Task<IActionResult> HitRedirect(string bait, string location, CancellationToken token)
    {
        await honeypotService.RecordHit(HttpContext, bait, Category, token: token);
        return Redirect(location);
    }
}
