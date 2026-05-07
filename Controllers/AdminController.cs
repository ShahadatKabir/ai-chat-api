using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IDataPersistenceService _persistence;
    private readonly IUserService _userService;
    private readonly IChatHistoryService _historyService;

    public AdminController(
        IConfiguration configuration,
        IDataPersistenceService persistence,
        IUserService userService,
        IChatHistoryService historyService)
    {
        _configuration = configuration;
        _persistence = persistence;
        _userService = userService;
        _historyService = historyService;
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var logPath = Path.Combine("data", "audit.log");
        if (!System.IO.File.Exists(logPath))
        {
            return Ok(new List<object>());
        }

        var lines = await System.IO.File.ReadAllLinesAsync(logPath);
        var totalLogs = lines.Length;
        var logs = lines
            .Reverse()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select((line, index) =>
            {
                var parts = line.Split('|');
                return new
                {
                    timestamp = parts.Length > 0 ? parts[0] : "",
                    userId = parts.Length > 1 ? parts[1] : "",
                    action = parts.Length > 2 ? parts[2] : "",
                    details = parts.Length > 3 ? parts[3] : ""
                };
            })
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalLogs,
            totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize),
            logs
        });
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var history = _historyService.GetHistory();
        var sessions = _historyService.GetAllSessions();

        return Ok(new
        {
            totalMessages = history.Count,
            totalSessions = sessions.Count,
            earliestMessage = history.FirstOrDefault()?.Timestamp,
            latestMessage = history.LastOrDefault()?.Timestamp
        });
    }

    [HttpPost("clear-all-data")]
    public IActionResult ClearAllData([FromBody] ClearDataRequest request)
    {
        if (request?.ConfirmPassword != "CONFIRM_DELETE_ALL")
        {
            return BadRequest(new { error = "Invalid confirmation password." });
        }

        _historyService.Clear();
        return Ok(new { message = "All data cleared." });
    }
}

public class ClearDataRequest
{
    public string ConfirmPassword { get; set; } = string.Empty;
}