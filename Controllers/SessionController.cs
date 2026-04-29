using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly IChatHistoryService _historyService;

    public SessionController(IChatHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// Create a new conversation session
    /// </summary>
    [HttpPost]
    public IActionResult CreateSession([FromBody] CreateSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Title))
        {
            return BadRequest(new { error = "Title is required." });
        }

        var session = _historyService.CreateSession(request.Title);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
    }

    /// <summary>
    /// Get all conversation sessions
    /// </summary>
    [HttpGet]
    public IActionResult GetAllSessions()
    {
        var sessions = _historyService.GetAllSessions();
        return Ok(sessions);
    }

    /// <summary>
    /// Get a specific session by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetSession(string id)
    {
        var session = _historyService.GetSession(id);
        if (session == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        return Ok(session);
    }

    /// <summary>
    /// Rename a conversation session
    /// </summary>
    [HttpPatch("{id}")]
    public IActionResult UpdateSession(string id, [FromBody] UpdateSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Title))
        {
            return BadRequest(new { error = "Title is required." });
        }

        if (!_historyService.UpdateSessionTitle(id, request.Title))
        {
            return NotFound(new { error = "Session not found." });
        }

        return Ok(_historyService.GetSession(id));
    }

    /// <summary>
    /// Switch to a specific session
    /// </summary>
    [HttpPost("{id}/switch")]
    public IActionResult SwitchSession(string id)
    {
        var session = _historyService.GetSession(id);
        if (session == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        _historyService.SetCurrentSession(id);
        return Ok(new { message = "Session switched successfully.", sessionId = id });
    }

    /// <summary>
    /// Get chat history for a specific session
    /// </summary>
    [HttpGet("{id}/history")]
    public IActionResult GetSessionHistory(string id)
    {
        if (_historyService.GetSession(id) == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        var history = _historyService.GetSessionHistory(id);
        return Ok(history);
    }

    /// <summary>
    /// Get paginated chat history for a specific session
    /// </summary>
    [HttpGet("{id}/history/paged")]
    public IActionResult GetSessionHistoryPage(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (_historyService.GetSession(id) == null)
        {
            return NotFound(new { error = "Session not found." });
        }

        return Ok(_historyService.GetHistoryPage(page, pageSize, id));
    }

    /// <summary>
    /// Delete a session (cannot delete current session)
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult DeleteSession(string id)
    {
        if (id == _historyService.GetCurrentSessionId())
        {
            return BadRequest(new { error = "Cannot delete the current active session." });
        }

        _historyService.DeleteSession(id);
        return NoContent();
    }
}

public class CreateSessionRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateSessionRequest
{
    public string Title { get; set; } = string.Empty;
}
