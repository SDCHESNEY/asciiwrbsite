using System;
using System.Collections.Generic;
using System.Diagnostics;
using AsciiSite.Server;
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
        var payload = await PlainTextResponseWriter.BuildAsync(hero, about, context.RequestAborted);
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(payload, context.RequestAborted);
        return;
    }

    await next();
});

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/text", async (IAsciiArtProvider asciiArtProvider, IAboutContentProvider aboutContentProvider, CancellationToken cancellationToken) =>
    {
        var payload = await PlainTextResponseWriter.BuildAsync(asciiArtProvider, aboutContentProvider, cancellationToken);
        return Results.Text(payload, "text/plain");
    })
    .WithName("GetPlainText")
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
