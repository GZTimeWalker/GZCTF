using Npgsql;

namespace GZCTF.Extensions.Startup;

internal sealed record DatabaseConnectionDiagnostic(string Host, string Database)
{
    internal string LogValue => $"Host={Host};Database={Database}";

    internal static DatabaseConnectionDiagnostic Parse(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new("unknown", "unknown");

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return new(Sanitize(builder.Host), Sanitize(builder.Database));
        }
        catch (ArgumentException)
        {
            return new("invalid", "unknown");
        }
    }

    private static string Sanitize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Replace('\r', ' ').Replace('\n', ' ');
}
