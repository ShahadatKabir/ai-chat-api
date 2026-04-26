using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChatHistoryService
{
    // Existing
    Task AddAsync(ChatHistoryItem item);
    IReadOnlyList<ChatHistoryItem> GetHistory();
    void Clear();

    // Session Management
    ChatSession CreateSession(string title);
    IReadOnlyCollection<ChatSession> GetAllSessions();
    ChatSession GetSession(string sessionId);
    void SetCurrentSession(string sessionId);
    string GetCurrentSessionId();
    IReadOnlyList<ChatHistoryItem> GetSessionHistory(string sessionId);
    void DeleteSession(string sessionId);

    // Search
    ChatSearchResult SearchMessages(string keyword, string sessionId = null);
    ChatSearchResult SearchByDateRange(DateTime startDate, DateTime endDate, string sessionId = null);

    // Analytics
    ChatAnalytics GetAnalytics(string sessionId = null);
}
