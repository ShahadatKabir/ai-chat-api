using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GeminiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(IHttpClientFactory factory, IConfiguration configuration)
    {
        _httpClient = factory.CreateClient();
        _configuration = configuration;
        _apiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _model = configuration["Gemini:Model"] ?? "gemini-pro";

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Set appsettings.json or the GEMINI_API_KEY environment variable.");
        }
    }

    public async Task<string> GenerateContentAsync(string prompt, double? temperature = null, int? maxTokens = null)
    {
        var response = await GenerateContentWithDetailsAsync(prompt, temperature, maxTokens);
        
        if (response is JObject json)
        {
            return json["candidates"]?[0]?["content"]?[0]?["text"]?.ToString() ?? response.ToString();
        }

        return response.ToString();
    }

    public async Task<object> GenerateContentWithDetailsAsync(string prompt, double? temperature = null, int? maxTokens = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            temperature = temperature ?? 0.7,
            maxOutputTokens = maxTokens ?? 1000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {content}");
        }

        return JsonConvert.DeserializeObject(content);
    }
}
