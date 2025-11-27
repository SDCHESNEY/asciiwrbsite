namespace AsciiSite.Shared.Localization;

/// <summary>
/// Provides display metadata and localized hero strings for a given culture.
/// </summary>
public sealed class LocalizationCulture
{
    public string Culture { get; init; } = "en";
    public string DisplayName { get; init; } = "English";
    public HeroLocalization? Hero { get; init; }
        = new("en", "ASCII-first storytelling for both browsers and curl.", "Explore the roadmap", "/docs/roadmap");
}
