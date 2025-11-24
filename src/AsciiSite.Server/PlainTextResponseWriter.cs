using System.Linq;
using System.Text;
using AsciiSite.Shared.Blog;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;

namespace AsciiSite.Server;

internal static class PlainTextResponseWriter
{
    private const int WrapWidth = 78;
    private const int BlogSummaryLimit = 3;

    public static async Task<string> BuildAsync(
        IAsciiArtProvider asciiArtProvider,
        IAboutContentProvider aboutContentProvider,
        IBlogPostProvider blogPostProvider,
        CancellationToken cancellationToken)
    {
        var hero = asciiArtProvider.GetHero();
        var about = await aboutContentProvider.GetAsync(cancellationToken);
        var blogSummaries = await blogPostProvider.GetSummariesAsync(cancellationToken);

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

        AppendBlogSection(builder, blogSummaries);

        builder.AppendLine();
        builder.AppendLine("Powered by ASCII Site. curl /text for this view.");

        return builder.ToString();
    }

    private static void AppendBlogSection(StringBuilder builder, IReadOnlyList<BlogPostSummary> summaries)
    {
        builder.AppendLine();
        builder.AppendLine("BLOG");

        if (summaries.Count == 0)
        {
            builder.AppendLine("No posts yet. Add markdown under content/blog to publish updates.");
            return;
        }

        foreach (var summary in summaries.Take(BlogSummaryLimit))
        {
            builder.AppendLine($"- {summary.Title} ({summary.PublishedOn:yyyy-MM-dd})");
            foreach (var line in Wrap(summary.Summary, WrapWidth - 2))
            {
                builder.Append("  ");
                builder.AppendLine(line);
            }

            builder.AppendLine($"  Read: /blog/{summary.Slug}");
            builder.AppendLine();
        }
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
