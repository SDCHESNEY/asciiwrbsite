namespace AsciiSite.Shared.Localization;

public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";

    public string DefaultCulture { get; init; } = "en";

    public List<LocalizationCulture> SupportedCultures { get; } = new();
}
