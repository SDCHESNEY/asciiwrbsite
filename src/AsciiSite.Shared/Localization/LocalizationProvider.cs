using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace AsciiSite.Shared.Localization;

public sealed class LocalizationProvider : ILocalizationProvider
{
    private readonly IOptionsMonitor<LocalizationOptions> _options;

    public LocalizationProvider(IOptionsMonitor<LocalizationOptions> options)
    {
        _options = options;
    }

    public HeroLocalization GetHeroLocalization(string? culture = null)
    {
        var current = _options.CurrentValue;
        var targetCulture = NormalizeCulture(culture) ?? current.DefaultCulture;
        var match = FindCulture(targetCulture, current.SupportedCultures);

        if (match?.Hero is not null)
        {
            return match.Hero with { Culture = match.Culture };
        }

        var fallback = FindCulture(current.DefaultCulture, current.SupportedCultures)
            ?? current.SupportedCultures.FirstOrDefault();

        if (fallback?.Hero is not null)
        {
            return fallback.Hero with { Culture = fallback.Culture };
        }

        return new HeroLocalization(current.DefaultCulture, "ASCII-first storytelling for both browsers and curl.", "Explore the roadmap", "/docs/roadmap");
    }

    public IReadOnlyList<LocalizationCulture> GetSupportedCultures()
        => _options.CurrentValue.SupportedCultures
            .OrderBy(culture => culture.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static LocalizationCulture? FindCulture(string? culture, IEnumerable<LocalizationCulture> supported)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        return supported.FirstOrDefault(item => string.Equals(item.Culture, culture, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        return culture.Replace('_', '-');
    }
}
