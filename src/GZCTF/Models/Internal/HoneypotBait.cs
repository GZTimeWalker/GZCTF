namespace GZCTF.Models.Internal;

/// <summary>
/// Constants for every HTTP honeypot bait path. Routes and cross-reference
/// strings inside response bodies use these so a typo can't silently break the chain.
/// </summary>
public static class HoneypotBait
{
    // Source-control leakage bait
    public const string GitConfig = "/.git/config";
    public const string GitHead = "/.git/HEAD";
    public const string SvnWcDb = "/.svn/wc.db";

    // Filesystem detritus
    public const string DsStore = "/.DS_Store";

    // Credentials / config bait
    public const string Env = "/.env";
    public const string AwsCredentials = "/.aws/credentials";

    // CMS / DB admin bait
    public const string WpAdmin = "/wp-admin";
    public const string WpAdminSlash = "/wp-admin/";
    public const string WpLogin = "/wp-login.php";
    public const string PhpMyAdmin = "/phpmyadmin";
    public const string PhpMyAdminSlash = "/phpmyadmin/";
    public const string PhpMyAdminIndex = "/phpmyadmin/index.php";

    // Server / framework debug bait
    public const string ServerStatus = "/server-status";
    public const string Actuator = "/actuator";
    public const string ActuatorEnv = "/actuator/env";
    public const string ActuatorHealth = "/actuator/health";
    public const string Ignition = "/_ignition/execute-solution";
    public const string CgiLuci = "/cgi-bin/luci";

    // Backup file bait
    public const string BackupZip = "/backup.zip";
    public const string BackupTarGz = "/backup.tar.gz";
    public const string DatabaseSql = "/database.sql";

    // Chain-graph bait — endpoints referenced from other bait responses,
    // designed to lure a link-following scanner deeper into the trap web.
    public const string AdminPortalLogin = "/admin-portal/login";
    public const string AdminPortalDashboard = "/admin-portal/dashboard";
    public const string ApiInternalUsers = "/api/internal/users.json";
    public const string InternalDebugConsole = "/internal/debug-console";
    public const string DebugConsole = "/_debug/console";
    public const string WpBackupZip = "/wp-content/uploads/backup-2024-q3.zip";
    public const string DbExportPhp = "/db-export.php";
    public const string DbDumpSql = "/backups/db-dump.sql";
    public const string SitemapInternalXml = "/sitemap-internal.xml";
}
