using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _maxRequestsPerMinute = 60; // Configurable

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var clientInfo = _clients.GetOrAdd(clientId, new ClientRequestInfo());

        if (clientInfo.IsRateLimited())
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
            return;
        }

        clientInfo.RecordRequest();
        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Use IP address for rate limiting (in production, consider user ID for authenticated requests)
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class ClientRequestInfo
{
    private readonly ConcurrentQueue<DateTime> _requests = new();
    private readonly int _maxRequestsPerMinute = 60;

    public void RecordRequest()
    {
        _requests.Enqueue(DateTime.UtcNow);

        // Remove old requests
        while (_requests.TryPeek(out var oldest) && (DateTime.UtcNow - oldest).TotalMinutes > 1)
        {
            _requests.TryDequeue(out _);
        }
    }

    public bool IsRateLimited()
    {
        // Clean up old requests
        while (_requests.TryPeek(out var oldest) && (DateTime.UtcNow - oldest).TotalMinutes > 1)
        {
            _requests.TryDequeue(out _);
        }

        return _requests.Count >= _maxRequestsPerMinute;
    }
}