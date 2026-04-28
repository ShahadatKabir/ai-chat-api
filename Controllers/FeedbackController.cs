using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IChatFeedbackService _feedbackService;

    public FeedbackController(IChatFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { error = "Rating must be between 1 and 5." });
        }

        var feedback = new ChatFeedback
        {
            UserId = userId,
            SessionId = request.SessionId,
            MessageId = request.MessageId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        await _feedbackService.AddFeedbackAsync(feedback);
        return CreatedAtAction(nameof(GetUserFeedback), new { }, new { feedback.Id });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserFeedback()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var feedbacks = await _feedbackService.GetFeedbackByUserAsync(userId);
        var averageRating = await _feedbackService.GetAverageRatingAsync(userId);

        return Ok(new { feedbacks, averageRating });
    }
}

public class FeedbackRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}