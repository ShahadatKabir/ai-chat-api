using System;

public class ChatHistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserMessage { get; set; } = string.Empty;
    public string BotResponse { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; }
    public int MaxOutputTokens { get; set; }
}
