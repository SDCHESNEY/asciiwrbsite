namespace AsciiSite.Shared.Blog;

/// <summary>
/// Represents a full blog post aggregate with markdown and HTML projections.
/// </summary>
/// <param name="Slug">Stable identifier inferred from frontmatter or file name.</param>
/// <param name="Title">Display title rendered in the UI and RSS feed.</param>
/// <param name="PublishedOn">Publish date used for ordering.</param>
/// <param name="Summary">Short summary rendered in listings and curl mode.</param>
/// <param name="Tags">Normalized set of tags for filtering.</param>
/// <param name="Markdown">Raw markdown content as stored on disk.</param>
/// <param name="Html">Sanitized HTML rendered via Blazor.</param>
public sealed record BlogPost(
    string Slug,
    string Title,
    DateOnly PublishedOn,
    string Summary,
    IReadOnlyList<string> Tags,
    string Markdown,
    string Html);
