using Microsoft.JSInterop;

namespace AsciiSite.Client.Services;

public sealed class PreferencesStore
{
    private readonly IJSRuntime _jsRuntime;

    public PreferencesStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask<string?> GetAsync(string key)
        => _jsRuntime.InvokeAsync<string?>("asciiPrefs.getItem", key);

    public ValueTask SetAsync(string key, string value)
        => _jsRuntime.InvokeVoidAsync("asciiPrefs.setItem", key, value);

    public ValueTask RemoveAsync(string key)
        => _jsRuntime.InvokeVoidAsync("asciiPrefs.removeItem", key);
}
