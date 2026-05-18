using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public int MessageCount { get; set; } = 0;
    public bool IsPinned { get; set; } = false;
    public List<string> Tags { get; set; } = new();
}
