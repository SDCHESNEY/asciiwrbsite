using AsciiSite.Shared.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AsciiSite.Tests.Configuration;

public sealed class AsciiArtProviderTests
{
    [Fact]
    public void GetHero_UsesOptionsValues()
    {
        var options = new AsciiArtOptions
        {
            HeroLines = new List<string> { "ASCII" },
            Tagline = "Tag",
            CallToActionText = "CTA",
            CallToActionUrl = "/cta",
            Navigation = new List<SiteNavigationLink> { new("Docs", "/docs") }
        };

        var provider = new AsciiArtProvider(new TestOptionsSnapshot<AsciiArtOptions>(options));
        var hero = provider.GetHero();

        hero.HeroLines.Should().ContainSingle("ASCII");
        hero.Tagline.Should().Be("Tag");
        hero.CallToActionUrl.Should().Be("/cta");
        hero.Navigation.Should().ContainSingle(link => link.Text == "Docs");
    }

    [Fact]
    public void GetHero_FallsBackToDefaults_WhenMissingValues()
    {
        var options = new AsciiArtOptions
        {
            HeroLines = new List<string>(),
            Navigation = new List<SiteNavigationLink>()
        };

        var provider = new AsciiArtProvider(new TestOptionsSnapshot<AsciiArtOptions>(options));
        var hero = provider.GetHero();

        hero.HeroLines.Should().NotBeEmpty();
        hero.Navigation.Should().NotBeEmpty();
        hero.Tagline.Should().Be(AsciiArtDefaults.Tagline);
    }

    private sealed class TestOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class, new()
    {
        public TestOptionsSnapshot(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public T Get(string? name) => Value;
    }
}
