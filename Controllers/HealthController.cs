using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public HealthController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        var health = new HealthReport
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            Version = "1.0.0",
            Checks = new Dictionary<string, HealthCheckResult>
            {
                ["api"] = new HealthCheckResult { Status = "Healthy" },
                ["memory"] = new HealthCheckResult 
                { 
                    Status = "Healthy", 
                    Details = new Dictionary<string, object>
                    {
                        ["allocatedMB"] = GC.GetTotalMemory(false) / 1024 / 1024
                    }
                }
            }
        };

        return Ok(health);
    }

    [HttpGet("ready")]
    public IActionResult GetReadiness()
    {
        return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            memory = new
            {
                workingSetMB = process.WorkingSet64 / 1024 / 1024,
                privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024,
                gcCollections = GC.CollectionCount(0)
            },
            cpu = new
            {
                threads = process.Threads.Count
            },
            uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime()
        });
    }
}

public class HealthReport
{
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Uptime { get; set; }
    public string Version { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; }
}

public class HealthCheckResult
{
    public string Status { get; set; }
    public Dictionary<string, object> Details { get; set; }
}