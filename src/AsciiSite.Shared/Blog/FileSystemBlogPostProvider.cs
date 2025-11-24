using System.Text;
using Markdig;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AsciiSite.Shared.Blog;

/// <summary>
/// Loads markdown-based blog posts from content/blog with caching and dev-time file watching.
/// </summary>
public sealed class FileSystemBlogPostProvider : IBlogPostProvider, IDisposable
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileSystemBlogPostProvider> _logger;
    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _deserializer;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private IReadOnlyList<BlogPost>? _cache;
    private PhysicalFileProvider? _fileProvider;
    private IDisposable? _changeToken;
    private string? _blogDirectory;

    public FileSystemBlogPostProvider(IHostEnvironment environment, ILogger<FileSystemBlogPostProvider> logger)
    {
        _environment = environment;
        _logger = logger;
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<IReadOnlyList<BlogPostSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var posts = await LoadPostsAsync(cancellationToken);
        return posts
            .Select(post => new BlogPostSummary(post.Slug, post.Title, post.PublishedOn, post.Summary, post.Tags))
            .ToArray();
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var posts = await LoadPostsAsync(cancellationToken);
        return posts.FirstOrDefault(post => string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<BlogPost>> LoadPostsAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _cache) is { } cached)
        {
            return cached;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null)
            {
                return _cache;
            }

            var directory = ResolveBlogDirectory();
            if (directory is null)
            {
                _logger.LogWarning("Blog content directory not found under {Root}", _environment.ContentRootPath);
                _cache = Array.Empty<BlogPost>();
                return _cache;
            }

            var posts = await ReadPostsAsync(directory, cancellationToken);
            _cache = posts;
            _blogDirectory = directory;
            EnsureWatcher();
            return posts;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private void EnsureWatcher()
    {
        if (!string.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_blogDirectory is null)
        {
            return;
        }

        _fileProvider ??= new PhysicalFileProvider(_blogDirectory);
        _changeToken ??= ChangeToken.OnChange(
            () => _fileProvider.Watch("*.md"),
            () =>
            {
                _logger.LogInformation("Blog content changed; clearing cache");
                InvalidateCache();
            });
    }

    private void InvalidateCache()
    {
        Volatile.Write(ref _cache, null);
    }

    private async Task<IReadOnlyList<BlogPost>> ReadPostsAsync(string directory, CancellationToken cancellationToken)
    {
        var posts = new List<BlogPost>();
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var post = await ParsePostAsync(file, cancellationToken);
            if (post is null)
            {
                continue;
            }

            if (!seenSlugs.Add(post.Slug))
            {
                _logger.LogWarning("Duplicate blog slug {Slug} detected for file {File}. Skipping.", post.Slug, file);
                continue;
            }

            posts.Add(post);
        }

        return posts
            .OrderByDescending(post => post.PublishedOn)
            .ThenBy(post => post.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<BlogPost?> ParsePostAsync(string file, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellationToken);
            var (frontMatterRaw, markdownBody) = SplitFrontMatter(text);
            var frontMatter = ParseFrontMatter(frontMatterRaw);

            if (frontMatter is null)
            {
                _logger.LogWarning("Frontmatter missing or invalid for blog file {File}", file);
                return null;
            }

            if (string.IsNullOrWhiteSpace(frontMatter.Title))
            {
                _logger.LogWarning("Title missing in blog file {File}", file);
                return null;
            }

            if (string.IsNullOrWhiteSpace(frontMatter.Published) || !DateOnly.TryParse(frontMatter.Published, out var publishedOn))
            {
                _logger.LogWarning("Published date missing or invalid in blog file {File}", file);
                return null;
            }

            var slugSource = string.IsNullOrWhiteSpace(frontMatter.Slug)
                ? Path.GetFileNameWithoutExtension(file)
                : frontMatter.Slug;
            var slug = Slugify(slugSource);
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Slug could not be derived for blog file {File}", file);
                return null;
            }

            var tags = NormalizeTags(frontMatter.Tags);
            var summary = string.IsNullOrWhiteSpace(frontMatter.Summary)
                ? CreateSummary(markdownBody)
                : frontMatter.Summary!.Trim();
            var html = Markdown.ToHtml(markdownBody, _pipeline);

            return new BlogPost(
                slug,
                frontMatter.Title.Trim(),
                publishedOn,
                summary,
                tags,
                markdownBody,
                html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse blog file {File}", file);
            return null;
        }
    }

    private static (string? FrontMatter, string MarkdownBody) SplitFrontMatter(string content)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
        {
            return (null, content);
        }

        using var reader = new StringReader(content);
        var firstLine = reader.ReadLine();
        if (firstLine is null || !firstLine.StartsWith("---", StringComparison.Ordinal))
        {
            return (null, content);
        }

        var builder = new StringBuilder();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("---", StringComparison.Ordinal))
            {
                break;
            }

            builder.AppendLine(line);
        }

        var remaining = reader.ReadToEnd() ?? string.Empty;
        return (builder.ToString(), remaining.TrimStart('\r', '\n'));
    }

    private BlogPostFrontMatter? ParseFrontMatter(string? frontMatterRaw)
    {
        if (string.IsNullOrWhiteSpace(frontMatterRaw))
        {
            return null;
        }

        try
        {
            return _deserializer.Deserialize<BlogPostFrontMatter>(frontMatterRaw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize blog frontmatter");
            return null;
        }
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? rawTags)
    {
        if (rawTags is null)
        {
            return Array.Empty<string>();
        }

        return rawTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct()
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();
    }

    private static string CreateSummary(string markdown)
    {
        var paragraphs = markdown.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstParagraph = paragraphs.FirstOrDefault() ?? string.Empty;
        if (firstParagraph.Length <= 280)
        {
            return firstParagraph.Trim();
        }

        return string.Concat(firstParagraph.AsSpan(0, 280).TrimEnd(), "...");
    }

    private string? ResolveBlogDirectory()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "content", "blog");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string Slugify(string input)
    {
        var builder = new StringBuilder(input.Length);
        var lastWasDash = false;

        foreach (var character in input)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasDash = false;
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                if (!lastWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }
        }

        return builder.ToString().Trim('-');
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
        _fileProvider?.Dispose();
        _cacheLock.Dispose();
    }

    private sealed class BlogPostFrontMatter
    {
        public string? Title { get; init; }
        public string? Slug { get; init; }
        public string? Published { get; init; }
        public string? Summary { get; init; }
        public IEnumerable<string>? Tags { get; init; }
    }
}
