public class ChatResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; }
    public int MaxOutputTokens { get; set; }
    public object RawResponse { get; set; } = null!;
}
