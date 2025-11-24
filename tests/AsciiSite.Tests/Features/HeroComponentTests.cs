using AsciiSite.Client.Features.Ascii;
using AsciiSite.Shared.Configuration;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AsciiSite.Tests.Features;

public sealed class HeroComponentTests
{
    [Fact]
    public void Hero_RendersAsciiArtAndNavigation()
    {
        using var ctx = new BunitContext();
        var heroContent = new AsciiHeroContent(
            new[] { "ASCII" },
            "Tagline",
            "CTA",
            "/cta",
            new[] { new SiteNavigationLink("Docs", "/docs") }
        );

        ctx.Services.AddSingleton<IAsciiArtProvider>(new FakeAsciiArtProvider(heroContent));

        var cut = ctx.Render<Hero>();

        cut.Markup.Should().Contain("ASCII");
        cut.Markup.Should().Contain("Tagline");
        cut.Markup.Should().Contain("CTA");
        cut.FindAll("ul.nav-links li").Should().HaveCount(1);
    }

    private sealed class FakeAsciiArtProvider : IAsciiArtProvider
    {
        private readonly AsciiHeroContent _hero;

        public FakeAsciiArtProvider(AsciiHeroContent hero)
        {
            _hero = hero;
        }

        public AsciiHeroContent GetHero() => _hero;
    }
}
