using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IConfiguration _configuration;
    private readonly IChatHistoryService _historyService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(IAiService aiService, IConfiguration configuration, IChatHistoryService historyService, IHttpClientFactory httpClientFactory)
    {
        _aiService = aiService;
        _configuration = configuration;
        _historyService = historyService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Request body must include a non-empty prompt." });
            }

            // Determine which AI provider to use based on model parameter
            IAiService aiService = _aiService;
            var requestedModel = request.Model?.ToLower() ?? string.Empty;

            var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
                ? _historyService.GetCurrentSessionId()
                : request.SessionId;

            if (_historyService.GetSession(sessionId) == null)
            {
                return BadRequest(new { error = "Session not found." });
            }

            if (requestedModel.Contains("gemma"))
            {
                aiService = new GemmaService(_httpClientFactory, _configuration);
            }
            else
            {
                aiService = new GeminiService(_httpClientFactory, _configuration);
            }

            // Generate response
            var responseText = await aiService.GenerateContentAsync(
                request.Prompt,
                request.Temperature,
                request.MaxOutputTokens
            );

            var model = string.IsNullOrWhiteSpace(request.Model)
                ? _configuration["Gemini:Model"] ?? "gemini-pro"
                : request.Model;

            var chatResponse = new ChatResponse
            {
                ResponseText = responseText,
                Model = model,
                Temperature = request.Temperature,
                MaxOutputTokens = request.MaxOutputTokens
            };

            // Store in history
            var historyItem = new ChatHistoryItem
            {
                UserMessage = request.Prompt,
                BotResponse = responseText,
                Model = model,
                Temperature = request.Temperature,
                MaxOutputTokens = request.MaxOutputTokens,
                SessionId = sessionId
            };

            await _historyService.AddAsync(historyItem);

            chatResponse.MessageId = historyItem.Id;
            chatResponse.SessionId = historyItem.SessionId;

            return Ok(chatResponse);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: 500);
        }
        catch (HttpRequestException ex)
        {
            return Problem(detail: $"AI API error: {ex.Message}", statusCode: 503);
        }
        catch (Exception ex)
        {
            return Problem(detail: $"An unexpected error occurred: {ex.Message}", statusCode: 500);
        }
    }
}
