public class ChatStreamChunk
{
    public string Delta { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public string? MessageId { get; set; }
    public string? SessionId { get; set; }
    public string? Model { get; set; }
    public string? Error { get; set; }
}
