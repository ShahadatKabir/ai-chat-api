public class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public int MaxOutputTokens { get; set; } = 256;
}
