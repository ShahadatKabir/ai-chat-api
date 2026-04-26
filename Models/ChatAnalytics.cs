using System;
using System.Collections.Generic;

public class ChatAnalytics
{
    public int TotalMessages { get; set; }
    public int TotalSessions { get; set; }
    public double AverageMessageLength { get; set; }
    public double AverageResponseLength { get; set; }
    public Dictionary<string, int> ModelUsageCount { get; set; } = new();
    public DateTime EarliestMessage { get; set; }
    public DateTime LatestMessage { get; set; }
    public string MostUsedModel { get; set; } = string.Empty;
}
