using System;
using System.Collections.Generic;

public class ChatSearchResult
{
    public int TotalMatches { get; set; }
    public List<ChatHistoryItem> Results { get; set; } = new();
}
