using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "API is working!", version = "1.0" });
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { uptime = "healthy", time = DateTime.UtcNow });
    }
}
