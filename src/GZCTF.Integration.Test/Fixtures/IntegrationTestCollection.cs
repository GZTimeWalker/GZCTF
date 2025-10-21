using Xunit;

namespace GZCTF.Integration.Test.Fixtures;

/// <summary>
/// Collection to ensure tests don't run in parallel (share the same factory instance)
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<GZCTFApplicationFactory>
{
}
