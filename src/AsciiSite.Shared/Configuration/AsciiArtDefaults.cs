namespace AsciiSite.Shared.Configuration;

public static class AsciiArtDefaults
{
    public static readonly IReadOnlyList<string> HeroLines = new[]
    {
        @"      ___     ___     ___     ___     ___ ",
        @"     /\__\   /\  \   /\  \   /\  \   /\__\",
        @"    /:/ _/_  \:\  \  \:\  \  \:\  \ /:/ _/_",
        @"   /:/ /\__\  \:\  \  \:\  \  \:\ /:/ /\__\",
        @"  /:/ /:/ _/_  \:\  \  \:\  \  \:\/:/ /:/ _/_",
        @" /:/_/:/ /\__\  \:\__\  \:\__\  \::/_/:/ /\__\",
        @" \:\/:/ /:/  /  /:/  /  /:/  /   \:\/:/ /:/  /",
        @"  \::/_/:/  /  /:/  /  /:/  /     \::/_/:/  / ",
        @"   \:\/:/  /   \:\/__/   \:\/__/       \:\/:/  /  ",
        @"    \::/  /     \::/  /    \::/  /        \::/  /   ",
        @"     \/__/       \/__/      \/__/          \/__/    "
    };

    public static readonly IReadOnlyList<SiteNavigationLink> Navigation = new List<SiteNavigationLink>
    {
        new("Blog", "/blog"),
        new("About", "/about"),
        new("GitHub", "https://github.com/SDCHESNEY")
    };

    public const string Tagline = "ASCII-first storytelling for both browsers and curl.";
    public const string CallToActionText = "Read the roadmap";
    public const string CallToActionUrl = "/docs/roadmap";
}
