using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace AsciiSite.Client.Services;

public sealed class ThemeManager
{
    private const string ThemeKey = "ascii:theme";
    private readonly PreferencesStore _store;
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = ThemePreference.Dark;
    private bool _initialized;

    public ThemeManager(PreferencesStore store, IJSRuntime jsRuntime)
    {
        _store = store;
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;

    public event Action? ThemeChanged;

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var stored = await _store.GetAsync(ThemeKey);
        if (ValidateTheme(stored))
        {
            _currentTheme = stored!;
        }

        await ApplyThemeAsync();
    }

    public async Task ToggleAsync()
    {
        await SetThemeAsync(_currentTheme == ThemePreference.Dark ? ThemePreference.Light : ThemePreference.Dark);
    }

    public async Task SetThemeAsync(string? theme)
    {
        var normalized = ValidateTheme(theme) ? theme! : ThemePreference.Dark;
        if (string.Equals(_currentTheme, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentTheme = normalized;
        await ApplyThemeAsync();
        await _store.SetAsync(ThemeKey, _currentTheme);
        ThemeChanged?.Invoke();
    }

    private ValueTask ApplyThemeAsync()
        => _jsRuntime.InvokeVoidAsync("asciiPrefs.setTheme", _currentTheme);

    private static bool ValidateTheme(string? value)
        => string.Equals(value, ThemePreference.Dark, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, ThemePreference.Light, StringComparison.OrdinalIgnoreCase);

    public static class ThemePreference
    {
        public const string Dark = "dark";
        public const string Light = "light";
    }
}
