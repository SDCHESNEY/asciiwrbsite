namespace AsciiSite.Shared.Localization;

/// <summary>
/// Represents localized hero content (tagline + CTA strings).
/// </summary>
public sealed record HeroLocalization
{
    public HeroLocalization()
    {
    }

    public HeroLocalization(string culture, string tagline, string callToActionText, string? callToActionUrl)
    {
        Culture = culture;
        Tagline = tagline;
        CallToActionText = callToActionText;
        CallToActionUrl = callToActionUrl;
    }

    public string Culture { get; set; } = "en";

    public string Tagline { get; set; } = "ASCII-first storytelling for both browsers and curl.";

    public string CallToActionText { get; set; } = "Explore the roadmap";

    public string? CallToActionUrl { get; set; } = "/docs/roadmap";
}
