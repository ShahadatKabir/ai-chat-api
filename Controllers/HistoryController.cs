using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpDelete]
    public IActionResult ClearHistory()
    {
        _historyService.Clear();
        return NoContent();
    }
}
