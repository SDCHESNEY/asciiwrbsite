namespace AsciiSite.Shared.Localization;

public interface ILocalizationProvider
{
    HeroLocalization GetHeroLocalization(string? culture = null);

    IReadOnlyList<LocalizationCulture> GetSupportedCultures();
}
