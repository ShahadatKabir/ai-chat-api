using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _client;

    public ChatController(IHttpClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] dynamic data)
    {
        string apiKey = "AIzaSyC3p6sGDUxh0s-T8CbYxt8XqLb80G3c_B0";
        string prompt = data.prompt;

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var res = await _client.SendAsync(req);
        var result = await res.Content.ReadAsStringAsync();

        return Ok(result);
    }
}
