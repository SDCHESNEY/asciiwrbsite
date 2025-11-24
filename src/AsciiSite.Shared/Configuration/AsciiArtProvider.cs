using Microsoft.Extensions.Options;

namespace AsciiSite.Shared.Configuration;

public sealed class AsciiArtProvider : IAsciiArtProvider
{
    private readonly IOptionsSnapshot<AsciiArtOptions> _options;

    public AsciiArtProvider(IOptionsSnapshot<AsciiArtOptions> options)
    {
        _options = options;
    }

    public AsciiHeroContent GetHero()
    {
        var current = _options.Value;

        var heroLines = (current.HeroLines.Count > 0 ? current.HeroLines : AsciiArtDefaults.HeroLines)
            .Select(line => line.TrimEnd('\r', '\n'))
            .ToArray();

        var navigation = (current.Navigation.Count > 0 ? current.Navigation : AsciiArtDefaults.Navigation)
            .Select(link => new SiteNavigationLink(link.Text.Trim(), link.Url.Trim()))
            .ToArray();

        var tagLine = string.IsNullOrWhiteSpace(current.Tagline) ? AsciiArtDefaults.Tagline : current.Tagline.Trim();
        var ctaText = string.IsNullOrWhiteSpace(current.CallToActionText) ? AsciiArtDefaults.CallToActionText : current.CallToActionText.Trim();
        var ctaUrl = string.IsNullOrWhiteSpace(current.CallToActionUrl) ? AsciiArtDefaults.CallToActionUrl : current.CallToActionUrl.Trim();

        return new AsciiHeroContent(heroLines, tagLine, ctaText, ctaUrl, navigation);
    }
}
