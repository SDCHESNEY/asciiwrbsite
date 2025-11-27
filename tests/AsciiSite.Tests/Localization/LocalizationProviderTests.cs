using AsciiSite.Shared.Localization;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AsciiSite.Tests.Localization;

public sealed class LocalizationProviderTests
{
    [Fact]
    public void GetHeroLocalization_ReturnsRequestedCulture()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            SupportedCultures =
            {
                new LocalizationCulture
                {
                    Culture = "en",
                    DisplayName = "English",
                    Hero = new HeroLocalization("en", "Hello", "Explore", "/docs")
                },
                new LocalizationCulture
                {
                    Culture = "es",
                    DisplayName = "Español",
                    Hero = new HeroLocalization("es", "Hola", "Explorar", "/docs")
                }
            }
        });

        var provider = new LocalizationProvider(new OptionsMonitorStub<LocalizationOptions>(options));

        var hero = provider.GetHeroLocalization("es-MX");

        hero.Culture.Should().Be("es");
        hero.Tagline.Should().Be("Hola");
    }

    [Fact]
    public void GetHeroLocalization_FallsBackToDefault()
    {
        var options = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            SupportedCultures =
            {
                new LocalizationCulture
                {
                    Culture = "en",
                    DisplayName = "English",
                    Hero = new HeroLocalization("en", "Hello", "Explore", "/docs")
                }
            }
        });

        var provider = new LocalizationProvider(new OptionsMonitorStub<LocalizationOptions>(options));

        var hero = provider.GetHeroLocalization("fr");

        hero.Culture.Should().Be("en");
        hero.Tagline.Should().Be("Hello");
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
        where T : class
    {
        private readonly T _value;

        public OptionsMonitorStub(IOptions<T> options)
        {
            _value = options.Value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
