namespace DroneMesh3D.Api.Tests.Integration;

/// <summary>
///     xUnit collection definition that groups all integration tests
///     so they share a single <see cref="DroneMesh3DApiFactory" /> instance
///     and run sequentially (no parallel DB conflicts).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<DroneMesh3DApiFactory>
{
    public const string Name = "Integration";
}
