using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Tunora.API.Tests;

public class AuthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetInstances_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/instances");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalyticsOverview_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/analytics/overview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
