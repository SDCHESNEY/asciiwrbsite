extern alias server;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsciiSite.Tests.Integration;

public sealed class MetricsEndpointTests : IClassFixture<WebApplicationFactory<server::Program>>
{
    private readonly HttpClient _client;

    public MetricsEndpointTests(WebApplicationFactory<server::Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Metrics_ReturnsPrometheusPayload()
    {
        var response = await _client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("ascii_http_requests_total");
        payload.Should().Contain("ascii_plaintext_requests_total");
    }
}
