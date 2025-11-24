extern alias server;

using System.Net;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsciiSite.Tests.Integration;

public sealed class RssFeedTests : IClassFixture<WebApplicationFactory<server::Program>>
{
    private readonly WebApplicationFactory<server::Program> _factory;

    public RssFeedTests(WebApplicationFactory<server::Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FeedEndpoint_ReturnsRssDocument()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/rss+xml");
        var xml = await response.Content.ReadAsStringAsync();
        var document = XDocument.Parse(xml);
        document.Root!.Name.LocalName.Should().Be("rss");
        document.Descendants("item").Should().NotBeEmpty();
    }
}
