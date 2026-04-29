using System.Collections.Generic;

public class PagedChatHistoryResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<ChatHistoryItem> Items { get; set; } = new List<ChatHistoryItem>();
}
