using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IUserRateLimitService _rateLimitService;
    private readonly int _maxRequestsPerMinute;
    private readonly IConfiguration _configuration;

    public RateLimitingMiddleware(RequestDelegate next, IUserRateLimitService rateLimitService, IConfiguration configuration)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _configuration = configuration;
        _maxRequestsPerMinute = configuration.GetValue<int>("RateLimiting:MaxRequestsPerMinute", 60);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var limitInfo = _rateLimitService.GetRateLimitInfo(clientId, _maxRequestsPerMinute);

        if (limitInfo.IsLimited)
        {
            context.Response.Headers["X-RateLimit-Limit"] = _maxRequestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
            return;
        }

        _rateLimitService.RecordRequest(clientId);
        context.Response.Headers["X-RateLimit-Limit"] = _maxRequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = limitInfo.RemainingRequests.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = "60";

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}