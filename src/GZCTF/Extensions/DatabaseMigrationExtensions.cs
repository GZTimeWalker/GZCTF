using GZCTF.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Extensions;

public static class DatabaseMigrationExtensions
{
    public static DbContextOptionsBuilder UseMigrationNamespace(this DbContextOptionsBuilder optionsBuilder, IMigrationNamespace migrationNamespace)
    {
        var shardingWrapExtension = optionsBuilder.CreateOrGetExtension(migrationNamespace);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(shardingWrapExtension);
        return optionsBuilder;
    }

    private static MigrationNamespaceExtension CreateOrGetExtension(
        this DbContextOptionsBuilder optionsBuilder, IMigrationNamespace migrationNamespace)
        => optionsBuilder.Options.FindExtension<MigrationNamespaceExtension>() ??
           new MigrationNamespaceExtension(migrationNamespace);
}
