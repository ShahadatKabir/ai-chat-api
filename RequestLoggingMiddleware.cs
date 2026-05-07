using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("[{RequestId}] {Method} {Path} started", requestId, context.Request.Method, context.Request.Path);

        await _next(context);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation("[{RequestId}] {Method} {Path} completed in {Duration}ms with status {StatusCode}",
            requestId, context.Request.Method, context.Request.Path, duration.TotalMilliseconds, context.Response.StatusCode);
    }
}