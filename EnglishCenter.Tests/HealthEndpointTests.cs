using Microsoft.AspNetCore.Mvc.Testing;

namespace EnglishCenter.Tests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveHealthCheck_ReturnsOk_WithoutDatabaseDependency()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.IsSuccessStatusCode);
    }
}
