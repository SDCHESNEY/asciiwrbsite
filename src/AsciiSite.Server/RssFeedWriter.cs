using System.Linq;
using System.Text;
using System.Xml.Linq;
using AsciiSite.Shared.Blog;

namespace AsciiSite.Server;

internal static class RssFeedWriter
{
    private const int MaxItems = 20;

    public static string Build(Uri baseUri, IEnumerable<BlogPostSummary> posts)
    {
        var now = DateTimeOffset.UtcNow;
        var channel = new XElement("channel",
            new XElement("title", "ASCII Site Blog"),
            new XElement("link", Combine(baseUri, "/blog")),
            new XElement("description", "Markdown-driven updates from ASCII Site."),
            new XElement("lastBuildDate", now.ToString("r")));

        foreach (var post in posts.Take(MaxItems))
        {
            var itemLink = Combine(baseUri, $"/blog/{post.Slug}");
            channel.Add(new XElement("item",
                new XElement("title", post.Title),
                new XElement("link", itemLink),
                new XElement("guid", itemLink),
                new XElement("pubDate", post.PublishedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("r")),
                new XElement("description", new XCData(post.Summary))));
        }

        var document = new XDocument(new XElement("rss",
            new XAttribute("version", "2.0"),
            channel));

        using var writer = new Utf8StringWriter();
        document.Save(writer);
        return writer.ToString();
    }

    private static string Combine(Uri baseUri, string path)
    {
        var builder = new UriBuilder(baseUri)
        {
            Path = path.TrimStart('/')
        };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
