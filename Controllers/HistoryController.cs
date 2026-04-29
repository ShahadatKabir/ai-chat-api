using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System;

[ApiController]
[Route("api/chat/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IChatHistoryService _historyService;

    public HistoryController(IChatHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    public IActionResult GetHistory()
    {
        return Ok(_historyService.GetHistory());
    }

    [HttpGet("paged")]
    public IActionResult GetHistoryPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sessionId = null)
    {
        return Ok(_historyService.GetHistoryPage(page, pageSize, sessionId));
    }

    [HttpDelete("{messageId}")]
    public IActionResult DeleteMessage(string messageId)
    {
        if (!_historyService.DeleteMessage(messageId))
        {
            return NotFound(new { error = "Message not found." });
        }

        return NoContent();
    }

    [HttpDelete]
    public IActionResult ClearHistory()
    {
        _historyService.Clear();
        return NoContent();
    }

    /// <summary>
    /// Export chat history as JSON
    /// </summary>
    [HttpGet("export/json")]
    public IActionResult ExportAsJson([FromQuery] string sessionId = null)
    {
        var history = string.IsNullOrEmpty(sessionId)
            ? _historyService.GetHistory()
            : _historyService.GetSessionHistory(sessionId);

        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);

        return File(bytes, "application/json", $"chat_history_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
    }

    /// <summary>
    /// Export chat history as CSV
    /// </summary>
    [HttpGet("export/csv")]
    public IActionResult ExportAsCsv([FromQuery] string sessionId = null)
    {
        var history = string.IsNullOrEmpty(sessionId)
            ? _historyService.GetHistory()
            : _historyService.GetSessionHistory(sessionId);

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,UserMessage,BotResponse,Model,Temperature,MaxOutputTokens");

        foreach (var item in history)
        {
            csv.AppendLine($"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{EscapeCsv(item.UserMessage)}\",\"{EscapeCsv(item.BotResponse)}\",\"{item.Model}\",\"{item.Temperature}\",\"{item.MaxOutputTokens}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"chat_history_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private string EscapeCsv(string text)
    {
        return text.Replace("\"", "\"\"");
    }
}
