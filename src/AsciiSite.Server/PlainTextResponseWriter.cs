using System.Text;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;

namespace AsciiSite.Server;

internal static class PlainTextResponseWriter
{
    private const int WrapWidth = 78;

    public static async Task<string> BuildAsync(IAsciiArtProvider asciiArtProvider, IAboutContentProvider aboutContentProvider, CancellationToken cancellationToken)
    {
        var hero = asciiArtProvider.GetHero();
        var about = await aboutContentProvider.GetAsync(cancellationToken);

        var builder = new StringBuilder();

        foreach (var line in hero.HeroLines)
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();
        builder.AppendLine(hero.Tagline);
        builder.AppendLine($"[{hero.CallToActionText}] -> {hero.CallToActionUrl}");
        builder.AppendLine();
        builder.AppendLine("NAVIGATION");

        foreach (var link in hero.Navigation)
        {
            builder.AppendLine($"- {link.Text} :: {link.Url}");
        }

        builder.AppendLine();
        builder.AppendLine("ABOUT");

        var summary = string.IsNullOrWhiteSpace(about.Summary)
            ? "Update content/about.md to share your story."
            : about.Summary;

        foreach (var line in Wrap(summary, WrapWidth))
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();
        builder.AppendLine("Powered by ASCII Site. curl /text for this view.");

        return builder.ToString();
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length >= width)
            {
                if (currentLine.Length > 0)
                {
                    yield return currentLine.ToString().TrimEnd();
                    currentLine.Clear();
                }

                foreach (var chunk in ChunkWord(word, width))
                {
                    yield return chunk;
                }

                continue;
            }

            if (currentLine.Length + word.Length + 1 > width)
            {
                yield return currentLine.ToString().TrimEnd();
                currentLine.Clear();
            }

            currentLine.Append(word).Append(' ');
        }

        if (currentLine.Length > 0)
        {
            yield return currentLine.ToString().TrimEnd();
        }
    }

    private static IEnumerable<string> ChunkWord(string word, int width)
    {
        for (var i = 0; i < word.Length; i += width)
        {
            var length = Math.Min(width, word.Length - i);
            yield return word.Substring(i, length);
        }
    }
}
