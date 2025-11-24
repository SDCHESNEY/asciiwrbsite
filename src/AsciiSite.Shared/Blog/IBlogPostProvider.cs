namespace AsciiSite.Shared.Blog;

public interface IBlogPostProvider
{
    /// <summary>
    /// Returns ordered blog summaries (most recent first) for listing pages.
    /// </summary>
    Task<IReadOnlyList<BlogPostSummary>> GetSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full blog post for the provided slug.
    /// </summary>
    Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
