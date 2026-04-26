using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IChatHistoryService _historyService;

    public SearchController(IChatHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// Search messages by keyword
    /// </summary>
    [HttpGet("keyword")]
    public IActionResult SearchByKeyword([FromQuery] string keyword, [FromQuery] string sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { error = "Keyword is required." });
        }

        var result = _historyService.SearchMessages(keyword, sessionId);
        return Ok(result);
    }

    /// <summary>
    /// Search messages by date range
    /// </summary>
    [HttpGet("daterange")]
    public IActionResult SearchByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string sessionId = null)
    {
        if (startDate > endDate)
        {
            return BadRequest(new { error = "Start date must be before end date." });
        }

        var result = _historyService.SearchByDateRange(startDate, endDate, sessionId);
        return Ok(result);
    }
}
