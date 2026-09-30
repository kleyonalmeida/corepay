using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Health;

[Collection("WebApiIntegration")]
public class HealthEndpointsTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(CorePayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyStatus()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthApiResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("healthy");
        body.Version.Should().Be("v1");
        body.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    private sealed record HealthApiResponse(
        string Status,
        string Version,
        DateTime Timestamp);
}
