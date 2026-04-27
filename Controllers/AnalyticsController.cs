using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Get a summary of conversations for a session
    /// </summary>
    [HttpGet("session/{sessionId}/summary")]
    public IActionResult GetSessionSummary(string sessionId)
    {
        var session = _historyService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        var history = _historyService.GetSessionHistory(sessionId);
        if (!history.Any())
        {
            return Ok(new { summary = "No messages in this session." });
        }

        var summary = GenerateSummary(history, session.Title);
        return Ok(new { session.Title, summary });
    }

    private string GenerateSummary(IReadOnlyList<ChatHistoryItem> history, string sessionTitle)
    {
        var historyList = history.ToList();
        var totalMessages = historyList.Count;
        var userMessages = historyList.Count(h => !string.IsNullOrEmpty(h.UserMessage));
        var avgUserLength = userMessages > 0 ? historyList.Average(h => h.UserMessage.Length) : 0;
        var avgResponseLength = historyList.Average(h => h.BotResponse.Length);
        var startTime = historyList.Min(h => h.Timestamp);
        var endTime = historyList.Max(h => h.Timestamp);
        var duration = endTime - startTime;

        var modelsUsed = historyList.Select(h => h.Model).Distinct().ToList();

        return $"Session '{sessionTitle}' contains {totalMessages} messages over {duration.TotalMinutes:F1} minutes. " +
               $"Average user message length: {avgUserLength:F0} characters. " +
               $"Average response length: {avgResponseLength:F0} characters. " +
               $"Models used: {string.Join(", ", modelsUsed)}.";
    }
}
