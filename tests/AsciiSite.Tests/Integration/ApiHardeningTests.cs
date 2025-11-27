extern alias server;

using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsciiSite.Tests.Integration;

public sealed class ApiHardeningTests : IClassFixture<WebApplicationFactory<server::Program>>
{
    private readonly HttpClient _client;

    public ApiHardeningTests(WebApplicationFactory<server::Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact(DisplayName = "Health endpoint returns 200 OK")]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Security headers applied to responses")]
    public async Task SecurityHeaders_AppliedToResponses()
    {
        var response = await _client.GetAsync("/weatherforecast");

        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.GetValues("Content-Security-Policy")
            .Should().Contain(header => header.Contains("default-src 'self'", StringComparison.Ordinal));
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Permissions-Policy");
        response.Headers.Should().ContainKey("Cross-Origin-Opener-Policy");
        response.Headers.Should().ContainKey("Cross-Origin-Resource-Policy");
    }

    [Fact(DisplayName = "Correlation Id header propagates")]
    public async Task CorrelationId_PreservedAcrossRequests()
    {
        var expected = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", expected);

        var response = await _client.SendAsync(request);

        response.Headers.Should().ContainKey("X-Correlation-Id");
        response.Headers.GetValues("X-Correlation-Id").Should().Contain(expected);
    }
}
