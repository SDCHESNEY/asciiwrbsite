using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsciiSite.Tests.Integration;

public sealed class PlainTextEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlainTextEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TextEndpoint_ReturnsPlainTextPayload()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/text");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("NAVIGATION");
        payload.Should().Contain("ABOUT");
    }

    [Fact]
    public async Task RootRequestWithTextAccept_ReturnsPlainText()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/plain");

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Powered by ASCII Site");
    }
}
