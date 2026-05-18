using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Rate-limit status for the current client/IP.
/// </summary>
[ApiController]
[Route("api/ratelimit")]
public class RateLimitController : ControllerBase
{
    private readonly IUserRateLimitService _rateLimitService;
    private readonly IConfiguration _configuration;

    public RateLimitController(IUserRateLimitService rateLimitService, IConfiguration configuration)
    {
        _rateLimitService = rateLimitService;
        _configuration = configuration;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        var clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var maxPerMinute = _configuration.GetValue<int>("RateLimiting:MaxRequestsPerMinute", 60);
        var info = _rateLimitService.GetRateLimitInfo(clientId, maxPerMinute);
        return Ok(info);
    }
}
