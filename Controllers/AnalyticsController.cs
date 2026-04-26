using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IChatHistoryService _historyService;

    public AnalyticsController(IChatHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// Get overall chat analytics
    /// </summary>
    [HttpGet]
    public IActionResult GetAnalytics()
    {
        var analytics = _historyService.GetAnalytics();
        return Ok(analytics);
    }

    /// <summary>
    /// Get analytics for a specific session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public IActionResult GetSessionAnalytics(string sessionId)
    {
        var session = _historyService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        var analytics = _historyService.GetAnalytics(sessionId);
        return Ok(analytics);
    }
}
