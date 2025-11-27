using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using AsciiSite.Server;
using AsciiSite.Server.Diagnostics;
using AsciiSite.Server.Middleware;
using AsciiSite.Shared.Blog;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;
using AsciiSite.Shared.GitHub;
using AsciiSite.Shared.Localization;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "O";
});
builder.Logging.AddDebug();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Tags | ActivityTrackingOptions.Baggage;
});

var applicationInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
    {
        ConnectionString = applicationInsightsConnectionString,
        EnableAdaptiveSampling = false
    });
}

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/rss+xml", "text/plain" });
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.AddResponseCaching();
builder.Services.AddSingleton<RequestMetrics>();
builder.Services.Configure<AsciiArtOptions>(builder.Configuration.GetSection(AsciiArtOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AsciiArtOptions>, AsciiArtOptionsValidator>();
builder.Services.AddScoped<IAsciiArtProvider, AsciiArtProvider>();
builder.Services.AddScoped<IAboutContentProvider, FileSystemAboutContentProvider>();
builder.Services.AddSingleton<IBlogPostProvider, FileSystemBlogPostProvider>();
builder.Services.Configure<GitHubRepoOptions>(builder.Configuration.GetSection(GitHubRepoOptions.SectionName));
builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection(LocalizationOptions.SectionName));
builder.Services.AddHttpClient<IGitHubRepoService, GitHubRepoService>(client =>
    {
        client.BaseAddress = new Uri("https://api.github.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AsciiSite.Server/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    });
builder.Services.AddSingleton<ILocalizationProvider, LocalizationProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler(static appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.Headers["X-Trace-Id"] = traceId;
        context.Response.ContentType = "application/problem+json";
        var problem = Results.Problem(
            title: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?> { ["traceId"] = traceId, ["detail"] = exceptionHandler?.Error.Message }
        );
        await problem.ExecuteAsync(context);
    });
});

app.UseResponseCompression();

app.Use(async (context, next) =>
{
    var metrics = context.RequestServices.GetRequiredService<RequestMetrics>();
    metrics.RecordRequest(context.Request.Path);

    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests");
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), camera=(), microphone=()");
    context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
    await next();
});

app.Use(async (context, next) =>
{
    var acceptsPlainText = HttpMethods.IsGet(context.Request.Method) && context.Request.Headers.Accept.Any(value =>
        value?.Contains("text/plain", StringComparison.OrdinalIgnoreCase) == true);

    if (acceptsPlainText && context.Request.Path == "/")
    {
        var hero = context.RequestServices.GetRequiredService<IAsciiArtProvider>();
        var about = context.RequestServices.GetRequiredService<IAboutContentProvider>();
        var blog = context.RequestServices.GetRequiredService<IBlogPostProvider>();
        var repos = context.RequestServices.GetRequiredService<IGitHubRepoService>();
        var localization = context.RequestServices.GetRequiredService<ILocalizationProvider>();
        var heroLocalization = localization.GetHeroLocalization(ResolveCulture(context));
        var payload = await PlainTextResponseWriter.BuildAsync(hero, about, blog, repos, heroLocalization, context.RequestAborted);
        context.Response.ContentType = "text/plain";
        var headers = context.Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromSeconds(30) };
        context.Response.Headers[HeaderNames.Vary] = "Accept";
        await context.Response.WriteAsync(payload, context.RequestAborted);
        return;
    }

    await next();
});

app.UseHttpsRedirection();
app.UseResponseCaching();

app.MapHealthChecks("/health");

app.MapGet("/text", async (HttpContext httpContext, IAsciiArtProvider asciiArtProvider, IAboutContentProvider aboutContentProvider, IBlogPostProvider blogPostProvider, IGitHubRepoService gitHubRepoService, ILocalizationProvider localizationProvider, CancellationToken cancellationToken) =>
    {
        var heroLocalization = localizationProvider.GetHeroLocalization(ResolveCulture(httpContext));
        var payload = await PlainTextResponseWriter.BuildAsync(asciiArtProvider, aboutContentProvider, blogPostProvider, gitHubRepoService, heroLocalization, cancellationToken);
        var headers = httpContext.Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromSeconds(30) };
        httpContext.Response.Headers[HeaderNames.Vary] = "Accept";
        return Results.Text(payload, "text/plain");
    })
    .WithName("GetPlainText")
    .WithMetadata(new ResponseCacheAttribute { Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept" })
    .WithOpenApi();

app.MapGet("/feed", async (HttpContext context, IBlogPostProvider blogPostProvider, CancellationToken cancellationToken) =>
    {
        var hostValue = context.Request.Host.HasValue ? context.Request.Host.Host : "localhost";
        if (Uri.CheckHostName(hostValue) == UriHostNameType.Unknown)
        {
            hostValue = "localhost";
        }

        var scheme = string.IsNullOrWhiteSpace(context.Request.Scheme) ? "https" : context.Request.Scheme;
        var portSegment = context.Request.Host.Port.HasValue && context.Request.Host.Port is not (80 or 443)
            ? $":{context.Request.Host.Port}"
            : string.Empty;
        var baseUri = new Uri($"{scheme}://{hostValue}{portSegment}/");
        var posts = await blogPostProvider.GetSummariesAsync(cancellationToken);
        var rss = RssFeedWriter.Build(baseUri, posts);
        var headers = context.Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromMinutes(5) };
        context.Response.Headers[HeaderNames.Vary] = "Accept";
        return Results.Text(rss, "application/rss+xml; charset=utf-8");
    })
    .WithName("GetRssFeed")
    .WithMetadata(new ResponseCacheAttribute { Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept" })
    .WithOpenApi();

app.MapGet("/metrics", (RequestMetrics metrics) => Results.Text(metrics.ToPrometheus(), "text/plain"))
    .WithName("GetMetrics")
    .WithOpenApi();

static string? ResolveCulture(HttpContext context)
{
    if (!context.Request.Headers.TryGetValue(HeaderNames.AcceptLanguage, out var values))
    {
        return null;
    }

    foreach (var entry in values.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
        var culture = entry.Split(';', 2)[0].Trim();
        if (!string.IsNullOrWhiteSpace(culture))
        {
            return culture;
        }
    }

    return null;
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program;
