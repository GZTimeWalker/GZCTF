namespace GZCTF.Migrations;

public interface IMigrationNamespace
{
    string Namespace { get; }
}

public class PostgreSQLMigrationNamespace : IMigrationNamespace
{
    public string Namespace => "GZCTF.Migrations.PostgreSQL";
}

public class MySQLMigrationNamespace : IMigrationNamespace
{
    public string Namespace => "GZCTF.Migrations.MySQL";
}

public class SQLServerMigrationNamespace : IMigrationNamespace
{
    public string Namespace => "GZCTF.Migrations.SQLServer";
}

public class SQLiteMigrationNamespace : IMigrationNamespace
{
    public string Namespace => "GZCTF.Migrations.SQLite";
}
