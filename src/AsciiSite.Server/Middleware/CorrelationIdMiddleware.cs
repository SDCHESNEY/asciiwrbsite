using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AsciiSite.Server.Middleware;

internal sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers.TryGetValue(HeaderName, out var values)
            ? values.ToString()
            : null;

        var correlationId = Validate(incoming) ? incoming! : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation_id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            _logger.LogDebug("Handling {Method} {Path} with correlation {CorrelationId}", context.Request.Method, context.Request.Path, correlationId);
            await _next(context);
        }
    }

    private static bool Validate(string? value) => !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
}
