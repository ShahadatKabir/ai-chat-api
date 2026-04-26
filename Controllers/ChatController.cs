using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly IChatHistoryService _historyService;

    public ChatController(IHttpClientFactory factory, IConfiguration configuration, IChatHistoryService historyService)
    {
        _client = factory.CreateClient();
        _configuration = configuration;
        _historyService = historyService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { error = "Request body must include a non-empty prompt." });
        }

        var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Problem(detail: "Gemini API key is not configured. Set appsettings.json or the GEMINI_API_KEY environment variable.", statusCode: 500);
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? _configuration["Gemini:Model"] ?? "gemini-pro"
            : request.Model;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = request.Prompt } }
                }
            },
            temperature = request.Temperature,
            maxOutputTokens = request.MaxOutputTokens
        };

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        var res = await _client.SendAsync(req);
        var result = await res.Content.ReadAsStringAsync();

        string responseText = result;
        try
        {
            var json = JObject.Parse(result);
            responseText = json["candidates"]?[0]?["content"]?[0]?["text"]?.ToString() ?? result;
        }
        catch
        {
            // Keep raw text if response is not JSON or shape is unexpected.
        }

        var chatResponse = new ChatResponse
        {
            ResponseText = responseText,
            Model = model,
            Temperature = request.Temperature,
            MaxOutputTokens = request.MaxOutputTokens,
            RawResponse = JsonConvert.DeserializeObject(result)
        };

        await _historyService.AddAsync(new ChatHistoryItem
        {
            UserMessage = request.Prompt,
            BotResponse = responseText,
            Model = model,
            Temperature = request.Temperature,
            MaxOutputTokens = request.MaxOutputTokens,
            SessionId = _historyService.GetCurrentSessionId()
        });

        return Ok(chatResponse);
    }
}
