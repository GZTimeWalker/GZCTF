using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace GZCTF.Migrations;

public class MultiMigrationDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc/>
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMigrationsScaffolder, MultiMigrationsScaffolder>();
    }
}
