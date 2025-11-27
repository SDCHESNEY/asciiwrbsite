using AsciiSite.Client.Features.Ascii;
using AsciiSite.Client.Services;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Localization;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

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
        ctx.Services.AddSingleton<IJSRuntime>(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<PreferencesStore>();
        ctx.Services.AddSingleton<ILocalizationProvider>(new FakeLocalizationProvider(heroContent));
        ctx.Services.AddSingleton<ILogger<LocalizationState>>(NullLogger<LocalizationState>.Instance);
        ctx.Services.AddSingleton<LocalizationState>();

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

    private sealed class FakeLocalizationProvider : ILocalizationProvider
    {
        private readonly HeroLocalization _hero;

        public FakeLocalizationProvider(AsciiHeroContent heroContent)
        {
            _hero = new HeroLocalization("en", heroContent.Tagline, heroContent.CallToActionText, heroContent.CallToActionUrl);
        }

        public HeroLocalization GetHeroLocalization(string? culture = null) => _hero;

        public IReadOnlyList<LocalizationCulture> GetSupportedCultures() =>
            new[]
            {
                new LocalizationCulture
                {
                    Culture = "en",
                    DisplayName = "English",
                    Hero = _hero
                }
            };
    }
}
