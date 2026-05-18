using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Streaming chat endpoints — responses are sent incrementally via Server-Sent Events (SSE).
/// </summary>
[ApiController]
[Route("api/chat/stream")]
[Authorize]
public class StreamingChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public StreamingChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a prompt and receive a streaming text response (SSE format).
    /// Each SSE event is a line: data: {"delta":"...","isDone":false}
    /// Final event:      data: {"isDone":true,"messageId":"..."}
    /// </summary>
    [HttpPost]
    public async Task Stream([FromBody] StreamingChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            Response.StatusCode = 400;
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = "Prompt is required." }));
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            return;
        }

        var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Response.StatusCode = 500;
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = "Gemini API key not configured. Set appsettings.json or GEMINI_API_KEY." }));
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            return;
        }

        var model = !string.IsNullOrWhiteSpace(request.Model)
            ? request.Model
            : _configuration["Gemini:Model"] ?? "gemini-pro";

        var httpClient = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?key={apiKey}&alt=sse";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = request.Prompt } } }
            },
            temperature = request.Temperature ?? 0.7,
            maxOutputTokens = request.MaxOutputTokens ?? 2048
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Response.StatusCode = 502;
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = $"AI API error {response.StatusCode}: {err}" }));
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                return;
            }

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Body.Flush();

            await using var upstreamStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(upstreamStream, Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    var jsonData = line[5..].Trim();
                    if (jsonData == "[DONE]") break;

                    try
                    {
                        var text = JObject.Parse(jsonData)?["candidates"]?[0]?["content"]?[0]?["text"]?.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            var chunk = new ChatStreamChunk
                            {
                                Delta = text,
                                IsDone = false,
                                Model = model
                            };
                            var payload = JsonSerializer.Serialize(chunk);
                            var bytes = Encoding.UTF8.GetBytes($"data: {payload}\n\n");
                            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                            await Response.Body.FlushAsync();
                        }
                    }
                    catch { /* skip malformed SSE chunk */ }
                }
            }

            var doneChunk = JsonSerializer.Serialize(new ChatStreamChunk { IsDone = true });
            var doneBytes = Encoding.UTF8.GetBytes($"data: {doneChunk}\n\n");
            await Response.Body.WriteAsync(doneBytes, 0, doneBytes.Length);
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                var errChunk = JsonSerializer.Serialize(new ChatStreamChunk { IsDone = true, Error = ex.Message });
                var errBytes = Encoding.UTF8.GetBytes($"data: {errChunk}\n\n");
                await Response.Body.WriteAsync(errBytes, 0, errBytes.Length);
            }
        }
    }
}

public class StreamingChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; }
    public string SessionId { get; set; } = string.Empty;
}
