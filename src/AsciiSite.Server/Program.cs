using System;
using System.Collections.Generic;
using System.Diagnostics;
using AsciiSite.Server;
using AsciiSite.Shared.Blog;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.Configure<AsciiArtOptions>(builder.Configuration.GetSection(AsciiArtOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AsciiArtOptions>, AsciiArtOptionsValidator>();
builder.Services.AddScoped<IAsciiArtProvider, AsciiArtProvider>();
builder.Services.AddScoped<IAboutContentProvider, FileSystemAboutContentProvider>();
builder.Services.AddSingleton<IBlogPostProvider, FileSystemBlogPostProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

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

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests");
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    await next();
});

app.Use(async (context, next) =>
{
    var acceptsPlainText = context.Request.Headers.Accept.Any(value =>
        value?.Contains("text/plain", StringComparison.OrdinalIgnoreCase) == true);

    if (acceptsPlainText && context.Request.Path == "/")
    {
        var hero = context.RequestServices.GetRequiredService<IAsciiArtProvider>();
        var about = context.RequestServices.GetRequiredService<IAboutContentProvider>();
        var blog = context.RequestServices.GetRequiredService<IBlogPostProvider>();
        var payload = await PlainTextResponseWriter.BuildAsync(hero, about, blog, context.RequestAborted);
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(payload, context.RequestAborted);
        return;
    }

    await next();
});

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/text", async (IAsciiArtProvider asciiArtProvider, IAboutContentProvider aboutContentProvider, IBlogPostProvider blogPostProvider, CancellationToken cancellationToken) =>
    {
        var payload = await PlainTextResponseWriter.BuildAsync(asciiArtProvider, aboutContentProvider, blogPostProvider, cancellationToken);
        return Results.Text(payload, "text/plain");
    })
    .WithName("GetPlainText")
    .WithOpenApi();

app.MapGet("/feed", async (HttpContext context, IBlogPostProvider blogPostProvider, CancellationToken cancellationToken) =>
    {
        var hostValue = context.Request.Host.HasValue ? context.Request.Host.Value : "localhost";
        var scheme = string.IsNullOrWhiteSpace(context.Request.Scheme) ? "https" : context.Request.Scheme;
        var baseUri = new Uri($"{scheme}://{hostValue}/");
        var posts = await blogPostProvider.GetSummariesAsync(cancellationToken);
        var rss = RssFeedWriter.Build(baseUri, posts);
        return Results.Text(rss, "application/rss+xml; charset=utf-8");
    })
    .WithName("GetRssFeed")
    .WithOpenApi();

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
