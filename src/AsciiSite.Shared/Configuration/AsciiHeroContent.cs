namespace AsciiSite.Shared.Configuration;

public sealed record AsciiHeroContent(
    IReadOnlyList<string> HeroLines,
    string Tagline,
    string CallToActionText,
    string CallToActionUrl,
    IReadOnlyList<SiteNavigationLink> Navigation);
