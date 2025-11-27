namespace AsciiSite.Shared.Localization;

/// <summary>
/// Provides display metadata and localized hero strings for a given culture.
/// </summary>
public sealed class LocalizationCulture
{
    public string Culture { get; set; } = "en";
    public string DisplayName { get; set; } = "English";
    public HeroLocalization? Hero { get; set; }
        = new("en", "ASCII-first storytelling for both browsers and curl.", "Explore the roadmap", "/docs/roadmap");
}
