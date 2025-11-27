extern alias server;

using System;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AsciiSite.Tests.Integration;

public sealed class PlainTextEndpointTests : IClassFixture<WebApplicationFactory<server::Program>>
{
    private readonly WebApplicationFactory<server::Program> _factory;

    public PlainTextEndpointTests(WebApplicationFactory<server::Program> factory)
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
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(30));
        response.Headers.Should().ContainKey("Vary");
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("NAVIGATION");
        payload.Should().Contain("ABOUT");
        payload.Should().Contain("BLOG");
        payload.Should().Contain("GITHUB");
    }

    [Fact]
    public async Task RootRequestWithTextAccept_ReturnsPlainText()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/plain");

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(30));
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Powered by ASCII Site");
        payload.Should().Contain("ASCII Site [C#]");
    }

    [Fact]
    public async Task TextEndpoint_HonorsAcceptLanguage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es");

        await using var scope = _factory.Services.CreateAsyncScope();
        var localizationProvider = scope.ServiceProvider.GetRequiredService<AsciiSite.Shared.Localization.ILocalizationProvider>();
        var heroLocalization = localizationProvider.GetHeroLocalization("es");
        heroLocalization.Tagline.Should().Be("Narrativas ASCII para navegadores y terminales.");

        var response = await client.GetAsync("/text");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Narrativas ASCII para navegadores y terminales.");
    }
}
