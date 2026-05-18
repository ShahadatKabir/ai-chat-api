using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Session extensions: pinning, tagging, and filtered session lookups.
/// </summary>
[ApiController]
[Route("api/sessions/extensions")]
[Authorize]
public class SessionExtensionsController : ControllerBase
{
    private readonly IChatHistoryService _historyService;

    public SessionExtensionsController(IChatHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>Pin a session to the top of the list.</summary>
    [HttpPost("{id}/pin")]
    public IActionResult PinSession(string id)
    {
        if (_historyService.PinSession(id))
        {
            return Ok(new { message = "Session pinned.", sessionId = id });
        }
        return NotFound(new { error = "Session not found." });
    }

    /// <summary>Unpin a session.</summary>
    [HttpPost("{id}/unpin")]
    public IActionResult UnpinSession(string id)
    {
        if (_historyService.UnpinSession(id))
        {
            return Ok(new { message = "Session unpinned.", sessionId = id });
        }
        return NotFound(new { error = "Session not found." });
    }

    /// <summary>Get sessions, pinned sessions first.</summary>
    [HttpGet]
    public IActionResult GetSessionsOrdered()
    {
        var sessions = _historyService.GetAllSessions()
            .OrderByDescending(s => s.IsPinned)
            .ThenByDescending(s => s.LastActiveAt)
            .ToList();
        return Ok(sessions);
    }

    /// <summary>Add a tag to a session.</summary>
    [HttpPost("{id}/tags")]
    public IActionResult AddTag(string id, [FromBody] TagRequestBody body)
    {
        if (string.IsNullOrWhiteSpace(body?.Tag))
        {
            return BadRequest(new { error = "Tag is required." });
        }
        if (_historyService.AddTagToSession(id, body.Tag))
        {
            return Ok(new { message = "Tag added.", tag = body.Tag, sessionId = id });
        }
        return NotFound(new { error = "Session not found or tag already exists." });
    }

    /// <summary>Remove a tag from a session.</summary>
    [HttpDelete("{id}/tags/{*tag}")]
    public IActionResult RemoveTag(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return BadRequest(new { error = "Tag is required." });
        }
        if (_historyService.RemoveTagFromSession(id, tag))
        {
            return Ok(new { message = "Tag removed.", tag, sessionId = id });
        }
        return NotFound(new { error = "Session not found or tag not found." });
    }

    /// <summary>Get all sessions that have a given tag.</summary>
    [HttpGet("by-tag/{tag}")]
    public IActionResult GetSessionsByTag(string tag)
    {
        var sessions = _historyService.GetSessionsByTag(tag);
        return Ok(sessions);
    }

    /// <summary>Get all unique tags used across sessions.</summary>
    [HttpGet("tags")]
    public IActionResult GetAllTags()
    {
        var allSessions = _historyService.GetAllSessions();
        var allTags = allSessions
            .SelectMany(s => s.Tags)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
        return Ok(new { tags = allTags });
    }
}

public class TagRequestBody
{
    public string Tag { get; set; } = string.Empty;
}
