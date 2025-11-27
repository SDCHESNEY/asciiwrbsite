using System;
using System.Text;
using System.Threading;

namespace AsciiSite.Server.Diagnostics;

internal sealed class RequestMetrics
{
    private long _totalRequests;
    private long _plainTextRequests;
    private long _rssRequests;
    private long _githubPageRequests;

    public void RecordRequest(string path)
    {
        Interlocked.Increment(ref _totalRequests);

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (path.Equals("/text", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _plainTextRequests);
        }
        else if (path.Equals("/feed", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _rssRequests);
        }
        else if (path.Equals("/github", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _githubPageRequests);
        }
    }

    public string ToPrometheus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# HELP ascii_http_requests_total Total HTTP requests handled by ASCII Site.");
        builder.AppendLine("# TYPE ascii_http_requests_total counter");
        builder.AppendLine($"ascii_http_requests_total{{service=\"AsciiSite.Server\"}} {TotalRequests}");

        builder.AppendLine("# HELP ascii_plaintext_requests_total Total /text requests served.");
        builder.AppendLine("# TYPE ascii_plaintext_requests_total counter");
        builder.AppendLine($"ascii_plaintext_requests_total {PlainTextRequests}");

        builder.AppendLine("# HELP ascii_rss_requests_total Total RSS feed requests served.");
        builder.AppendLine("# TYPE ascii_rss_requests_total counter");
        builder.AppendLine($"ascii_rss_requests_total {RssRequests}");

        builder.AppendLine("# HELP ascii_github_page_requests_total Total GitHub showcase page requests served.");
        builder.AppendLine("# TYPE ascii_github_page_requests_total counter");
        builder.AppendLine($"ascii_github_page_requests_total {GithubPageRequests}");

        builder.AppendLine($"# Generated {DateTimeOffset.UtcNow:O}");
        return builder.ToString();
    }

    private long TotalRequests => Interlocked.Read(ref _totalRequests);
    private long PlainTextRequests => Interlocked.Read(ref _plainTextRequests);
    private long RssRequests => Interlocked.Read(ref _rssRequests);
    private long GithubPageRequests => Interlocked.Read(ref _githubPageRequests);
}
