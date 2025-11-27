using System;
using System.Linq;
using System.Text;
using AsciiSite.Shared.Blog;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;
using AsciiSite.Shared.GitHub;
using AsciiSite.Shared.Localization;

namespace AsciiSite.Server;

internal static class PlainTextResponseWriter
{
    private const int WrapWidth = 78;
    private const int BlogSummaryLimit = 3;
    private const int RepoSummaryLimit = 4;

    public static async Task<string> BuildAsync(
        IAsciiArtProvider asciiArtProvider,
        IAboutContentProvider aboutContentProvider,
        IBlogPostProvider blogPostProvider,
        IGitHubRepoService gitHubRepoService,
        HeroLocalization heroLocalization,
        CancellationToken cancellationToken)
    {
        var hero = asciiArtProvider.GetHero();
        var about = await aboutContentProvider.GetAsync(cancellationToken);
        var blogSummaries = await blogPostProvider.GetSummariesAsync(cancellationToken);
        IReadOnlyList<GitHubRepo> repositories;

        try
        {
            repositories = await gitHubRepoService.GetRepositoriesAsync(cancellationToken);
        }
        catch
        {
            repositories = Array.Empty<GitHubRepo>();
        }

        var builder = new StringBuilder();

        foreach (var line in hero.HeroLines)
        {
            builder.AppendLine(line);
        }

        builder.AppendLine();
        var tagline = string.IsNullOrWhiteSpace(heroLocalization.Tagline) ? hero.Tagline : heroLocalization.Tagline;
        var callToActionText = string.IsNullOrWhiteSpace(heroLocalization.CallToActionText)
            ? hero.CallToActionText
            : heroLocalization.CallToActionText;
        var callToActionUrl = string.IsNullOrWhiteSpace(heroLocalization.CallToActionUrl)
            ? hero.CallToActionUrl
            : heroLocalization.CallToActionUrl;

        builder.AppendLine(tagline);
        builder.AppendLine($"[{callToActionText}] -> {callToActionUrl}");
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
        AppendGitHubSection(builder, repositories);

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

    private static void AppendGitHubSection(StringBuilder builder, IReadOnlyList<GitHubRepo> repositories)
    {
        builder.AppendLine();
        builder.AppendLine("GITHUB");

        if (repositories.Count == 0)
        {
            builder.AppendLine("No repositories loaded. Configure GitHub:Repositories in appsettings.json.");
            return;
        }

        foreach (var repo in repositories.Take(RepoSummaryLimit))
        {
            builder.AppendLine($"- {repo.DisplayName} [{repo.Language}] ⭐ {repo.Stars}");
            builder.AppendLine($"  {repo.Url}");

            if (!string.IsNullOrWhiteSpace(repo.Description))
            {
                foreach (var line in Wrap(repo.Description, WrapWidth - 2))
                {
                    builder.Append("  ");
                    builder.AppendLine(line);
                }
            }

            if (repo.Topics.Count > 0)
            {
                builder.AppendLine($"  Topics: {string.Join(", ", repo.Topics)}");
            }

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
