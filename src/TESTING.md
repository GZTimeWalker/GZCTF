# Running the backend tests

Run the commands below from `src/` so that the .NET CLI picks up [global.json](global.json). To check your environment against the current CI setup, see the [Integration Tests workflow](../.github/workflows/integration-tests.yml).

## Choose a test run

| What changed? | Start with | Services used |
| --- | --- | --- |
| A rule such as score calculation | The relevant tests in `GZCTF.Test` | The scoring tests run in process |
| An API or a database operation | `GZCTF.Integration.Test` in local mode (default) | Testcontainers starts PostgreSQL; the application uses local disk storage |
| Code that depends on Kubernetes or S3-compatible storage | The same integration suite in cloud mode | Testcontainers starts PostgreSQL, K3s and RustFS |

## Build and run

1. Install a .NET SDK compatible with `global.json` and check `dotnet --version` from this directory. For the full solution build, use the Node.js, pnpm and Aspire CLI setup in the workflow above.
2. Both test projects reference the web application. Its ImageSharp dependency checks for a Six Labors license at build time. If the build reports `No Six Labors license found`, configure your own valid license using the key or license file described in the build message. CI supplies its key through a repository secret. Keep local credentials out of commits and PR logs.
3. Restore and build the same Release configuration used by CI:

```sh
dotnet restore
dotnet build --no-restore -c Release
```

For a scoring change, run just that test class first. Omit the filter to run the full unit test project.

```sh
dotnet test GZCTF.Test/GZCTF.Test.csproj --no-build -c Release --filter "FullyQualifiedName~ScoringTests"
```

For an integration change, first run `docker info` and check that it returns **Server** details. The [test application factory](GZCTF.Integration.Test/Base/GZCTFApplicationFactory.cs) creates its own test database. In a fresh shell the following command uses local mode:

```sh
dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj --no-build -c Release
```

To exercise cloud mode in a POSIX shell, set the mode for that command only:

```sh
GZCTF_INTEGRATION_TEST_MODE=cloud dotnet test GZCTF.Integration.Test/GZCTF.Integration.Test.csproj --no-build -c Release
```

In a fresh PowerShell session, set `$env:GZCTF_INTEGRATION_TEST_MODE = 'cloud'` and run the same `dotnet test` command. Close that session to return to the default mode.

## Reading the result

- A successful run reports how many tests passed. For a filtered run, check that at least one test matched. Rebuild after editing source files before repeating a command with `--no-build`.
- If a suite fails during container startup, check that Docker is reachable and can pull the image named in the error. The startup log prints the selected test mode.
- If the build stops before test discovery, report the build error and which tests you could run. On GitHub, the **Integration Tests** workflow lists **Run Tests (Local Mode)** and **Run Tests (Cloud Mode)** separately. The local job includes the unit suite, and each job uploads its own test results when available.
