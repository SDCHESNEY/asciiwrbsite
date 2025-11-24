extern alias client;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsciiSite.Tests.Integration;

public sealed class BlogPageTests : IClassFixture<WebApplicationFactory<client::Program>>
{
    private readonly WebApplicationFactory<client::Program> _factory;

    public BlogPageTests(WebApplicationFactory<client::Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BlogIndex_ReturnsPosts()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/blog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Launching ASCII Site");
    }

    [Fact]
    public async Task BlogPost_ReturnsArticle()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/blog/launching-ascii-site");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("ASCII Site started as a tiny experiment");
    }
}
