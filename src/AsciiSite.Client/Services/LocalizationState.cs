using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsciiSite.Shared.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace AsciiSite.Client.Services;

public sealed class LocalizationState
{
    private const string CultureKey = "ascii:culture";
    private readonly ILocalizationProvider _provider;
    private readonly PreferencesStore _store;
    private readonly ILogger<LocalizationState> _logger;
    private bool _initialized;

    public LocalizationState(ILocalizationProvider provider, PreferencesStore store, ILogger<LocalizationState> logger)
    {
        _provider = provider;
        _store = store;
        _logger = logger;
    }

    public HeroLocalization CurrentHero { get; private set; } = new("en", string.Empty, string.Empty, null);

    public string CurrentCulture => CurrentHero.Culture;

    public IReadOnlyList<LocalizationCulture> Cultures => _provider.GetSupportedCultures();

    public event Action? Changed;

    public void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        CurrentHero = _provider.GetHeroLocalization();
        _initialized = true;
    }

    public async Task LoadPersistedAsync()
    {
        EnsureInitialized();
        try
        {
            var stored = await _store.GetAsync(CultureKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return;
            }

            if (string.Equals(stored, CurrentCulture, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await SetCultureInternalAsync(stored!, persist: false);
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogDebug(ex, "JS runtime disconnected while loading localization preferences.");
        }
    }

    public async Task SetCultureAsync(string? culture)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        await SetCultureInternalAsync(culture, persist: true);
    }

    private async Task SetCultureInternalAsync(string culture, bool persist)
    {
        var hero = _provider.GetHeroLocalization(culture);
        if (string.Equals(hero.Culture, CurrentCulture, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentHero = hero;
        Changed?.Invoke();

        if (persist)
        {
            await _store.SetAsync(CultureKey, hero.Culture);
        }
    }
}
