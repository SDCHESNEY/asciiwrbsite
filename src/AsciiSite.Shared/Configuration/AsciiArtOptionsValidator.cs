using Microsoft.Extensions.Options;

namespace AsciiSite.Shared.Configuration;

public sealed class AsciiArtOptionsValidator : IValidateOptions<AsciiArtOptions>
{
    private const int MaxLineLength = 80;
    private const int MaxLines = 20;
    private const int MaxTaglineLength = 160;
    private const int MaxCallToActionLength = 60;

    public ValidateOptionsResult Validate(string? name, AsciiArtOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("AsciiArt options cannot be null.");
        }

        if (options.HeroLines.Count is 0 or > MaxLines)
        {
            return ValidateOptionsResult.Fail($"HeroLines must contain between 1 and {MaxLines} entries.");
        }

        var tooLongLine = options.HeroLines.FirstOrDefault(line => line.Length > MaxLineLength);
        if (tooLongLine is not null)
        {
            return ValidateOptionsResult.Fail($"Hero line exceeds {MaxLineLength} characters: '{tooLongLine}'.");
        }

        if (string.IsNullOrWhiteSpace(options.Tagline) || options.Tagline.Length > MaxTaglineLength)
        {
            return ValidateOptionsResult.Fail($"Tagline is required and must be under {MaxTaglineLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(options.CallToActionText) || options.CallToActionText.Length > MaxCallToActionLength)
        {
            return ValidateOptionsResult.Fail($"CallToActionText is required and must be under {MaxCallToActionLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(options.CallToActionUrl))
        {
            return ValidateOptionsResult.Fail("CallToActionUrl is required.");
        }

        if (options.Navigation.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one navigation link is required.");
        }

        var invalidNav = options.Navigation.FirstOrDefault(link => string.IsNullOrWhiteSpace(link.Text) || string.IsNullOrWhiteSpace(link.Url));
        if (invalidNav is not null)
        {
            return ValidateOptionsResult.Fail("Navigation links require text and URL values.");
        }

        return ValidateOptionsResult.Success;
    }
}
