using Xunit;

namespace GZCTF.Integration.Test.Tests.Deployment;

public class ContainerContractTests
{
    [Fact]
    public void Dockerfile_exposes_runtime_contract()
    {
        var dockerfile = File.ReadAllText(FindRepoFile("src/GZCTF/Dockerfile"));

        Assert.Contains("EXPOSE 8080", dockerfile);
        Assert.Contains("http://localhost:3000/healthz", dockerfile);
        Assert.Contains("ENTRYPOINT [\"dotnet\", \"GZCTF.dll\"]", dockerfile);
    }

    [Fact]
    public void Server_and_application_keep_environment_contract()
    {
        var server = File.ReadAllText(FindRepoFile("src/GZCTF/Server.cs"));
        var appBuilder = File.ReadAllText(FindRepoFile("src/GZCTF/Extensions/Startup/AppBuilderExtensions.cs"));

        Assert.Contains("MetricPort = 3000", server);
        Assert.Contains("ServerPort = 8080", server);
        Assert.Contains("AddEnvironmentVariables(\"GZCTF_\")", appBuilder);
    }

    private static string FindRepoFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
    }
}
