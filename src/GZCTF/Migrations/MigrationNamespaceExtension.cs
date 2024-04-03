using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GZCTF.Migrations;
public class MigrationNamespaceExtension(IMigrationNamespace migrationNamespace) : IDbContextOptionsExtension
{
    public IMigrationNamespace MigrationNamespace { get; } = migrationNamespace;

    /// <inheritdoc/>
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(sp => MigrationNamespace);
    }

    /// <inheritdoc/>
    public void Validate(IDbContextOptions options)
    {
    }

    /// <inheritdoc/>
    public DbContextOptionsExtensionInfo Info => new MigrationNamespaceExtensionInfo(this);

    private class MigrationNamespaceExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        private readonly MigrationNamespaceExtension _migrationNamespaceExtension = (MigrationNamespaceExtension)extension;

        /// <inheritdoc/>
        public override int GetServiceProviderHashCode() => _migrationNamespaceExtension.MigrationNamespace.Namespace.GetHashCode();

        /// <inheritdoc/>
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        /// <inheritdoc/>
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        /// <inheritdoc/>
        public override bool IsDatabaseProvider => false;

        /// <inheritdoc/>
        public override string LogFragment => nameof(MigrationNamespaceExtension);
    }
}