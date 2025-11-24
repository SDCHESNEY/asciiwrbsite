using AsciiSite.Shared.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AsciiSite.Tests.Configuration;

public sealed class AsciiArtOptionsValidatorTests
{
    private readonly AsciiArtOptionsValidator _validator = new();

    [Fact]
    public void Validate_EmptyHeroLines_Fails()
    {
        var options = new AsciiArtOptions
        {
            HeroLines = new List<string>()
        };

        var result = _validator.Validate(AsciiArtOptions.SectionName, options);
        result.Should().BeOfType<ValidateOptionsResult>().Which.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_LongTagline_Fails()
    {
        var options = new AsciiArtOptions
        {
            Tagline = new string('a', 200)
        };

        var result = _validator.Validate(AsciiArtOptions.SectionName, options);
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var options = new AsciiArtOptions
        {
            HeroLines = new List<string> { "ASCII" },
            Tagline = "Test",
            CallToActionText = "Go",
            CallToActionUrl = "/go",
            Navigation = new List<SiteNavigationLink> { new("Docs", "/docs") }
        };

        var result = _validator.Validate(AsciiArtOptions.SectionName, options);
        result.Succeeded.Should().BeTrue();
    }
}
