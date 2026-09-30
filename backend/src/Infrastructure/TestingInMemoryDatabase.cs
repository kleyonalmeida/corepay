namespace Infrastructure;

/// <summary>
/// Stable in-memory database name per test host (WebApplicationFactory instance).
/// </summary>
internal sealed class TestingInMemoryDatabase
{
    public string Name { get; } = $"CorePayTests-{Guid.NewGuid():N}";
}
