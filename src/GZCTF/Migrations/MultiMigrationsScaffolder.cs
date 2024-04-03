using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace GZCTF.Migrations;

public class MultiMigrationsScaffolder(MigrationsScaffolderDependencies dependencies) : MigrationsScaffolder(dependencies)
{
    private readonly Type _contextType = dependencies.CurrentContext.Context.GetType();

    /// <inheritdoc/>
    protected override string GetDirectory(string projectDir, string? siblingFileName, string subnamespace)
    {
        var defaultDirectory = Path.Combine(projectDir, Path.Combine(subnamespace.Split('.')));

        if (siblingFileName != null)
        {
            if (!siblingFileName.StartsWith(_contextType.Name + "ModelSnapshot."))
            {
                var siblingPath = TryGetProjectFile(projectDir, siblingFileName);
                if (siblingPath != null)
                {
                    var lastDirectory = Path.GetDirectoryName(siblingPath)!;
                    if (!defaultDirectory.Equals(lastDirectory, StringComparison.OrdinalIgnoreCase))
                    {
#pragma warning disable EF1001 // Internal EF Core API usage.
                        Dependencies.OperationReporter.WriteVerbose(DesignStrings.ReusingNamespace(siblingFileName));
#pragma warning restore EF1001 // Internal EF Core API usage.

                        return lastDirectory;
                    }
                }
            }
        }

        return defaultDirectory;
    }
}
