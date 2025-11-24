namespace AsciiSite.Shared.Blog;

/// <summary>
/// Lightweight projection for listing blog posts with metadata only.
/// </summary>
/// <param name="Slug">Slug used for routing.</param>
/// <param name="Title">Display title.</param>
/// <param name="PublishedOn">Publish date.</param>
/// <param name="Summary">Excerpt or author-provided summary.</param>
/// <param name="Tags">Normalized list of tags.</param>
public sealed record BlogPostSummary(
    string Slug,
    string Title,
    DateOnly PublishedOn,
    string Summary,
    IReadOnlyList<string> Tags);
