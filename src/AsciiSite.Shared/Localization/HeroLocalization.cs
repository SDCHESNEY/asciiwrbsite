namespace AsciiSite.Shared.Localization;

/// <summary>
/// Represents localized hero content (tagline + CTA strings).
/// </summary>
/// <param name="Culture">IETF language tag (e.g., en, es, fr-FR).</param>
/// <param name="Tagline">Localized tagline.</param>
/// <param name="CallToActionText">Localized CTA text.</param>
/// <param name="CallToActionUrl">Optional CTA URL override.</param>
public sealed record HeroLocalization(
    string Culture,
    string Tagline,
    string CallToActionText,
    string? CallToActionUrl);
