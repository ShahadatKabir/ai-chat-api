using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly IChatHistoryService _historyService;

    public FavoritesController(IFavoritesService favoritesService, IChatHistoryService historyService)
    {
        _favoritesService = favoritesService;
        _historyService = historyService;
    }

    /// <summary>
    /// Add a message to favorites
    /// </summary>
    [HttpPost("{messageId}")]
    public async Task<IActionResult> AddFavorite(string messageId, [FromBody] FavoriteRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify message exists
        var history = _historyService.GetHistory();
        var message = history.FirstOrDefault(h => h.Id == messageId);
        if (message == null)
        {
            return NotFound(new { error = "Message not found." });
        }

        await _favoritesService.AddFavoriteAsync(userId, messageId, request?.Note ?? "");
        return Ok(new { message = "Added to favorites." });
    }

    /// <summary>
    /// Remove a message from favorites
    /// </summary>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> RemoveFavorite(string messageId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _favoritesService.RemoveFavoriteAsync(userId, messageId);
        return Ok(new { message = "Removed from favorites." });
    }

    /// <summary>
    /// Get user's favorite messages with full details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var favorites = await _favoritesService.GetUserFavoritesAsync(userId);
        var history = _historyService.GetHistory();

        var favoriteMessages = favorites
            .Select(f => new
            {
                Favorite = f,
                Message = history.FirstOrDefault(h => h.Id == f.MessageId)
            })
            .Where(fm => fm.Message != null)
            .ToList();

        return Ok(favoriteMessages);
    }

    /// <summary>
    /// Check if a message is favorited
    /// </summary>
    [HttpGet("{messageId}/status")]
    public async Task<IActionResult> IsFavorite(string messageId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var isFavorite = await _favoritesService.IsFavoriteAsync(userId, messageId);
        return Ok(new { isFavorite });
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

public class FavoriteRequest
{
    public string Note { get; set; } = string.Empty;
}