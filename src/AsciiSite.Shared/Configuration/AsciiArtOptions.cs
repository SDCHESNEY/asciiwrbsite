namespace AsciiSite.Shared.Configuration;

public sealed class AsciiArtOptions
{
    public const string SectionName = "AsciiArt";

    public List<string> HeroLines { get; init; } = new(AsciiArtDefaults.HeroLines);

    public string Tagline { get; set; } = AsciiArtDefaults.Tagline;

    public string CallToActionText { get; set; } = AsciiArtDefaults.CallToActionText;

    public string CallToActionUrl { get; set; } = AsciiArtDefaults.CallToActionUrl;

    public List<SiteNavigationLink> Navigation { get; init; } = new(AsciiArtDefaults.Navigation);
}
