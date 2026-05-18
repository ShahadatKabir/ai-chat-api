using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChatHistoryService
{
    // Existing
    Task AddAsync(ChatHistoryItem item);
    IReadOnlyList<ChatHistoryItem> GetHistory();
    PagedChatHistoryResult GetHistoryPage(int page, int pageSize, string sessionId = null);
    bool DeleteMessage(string messageId);
    void Clear();

    // Session Management
    ChatSession CreateSession(string title);
    IReadOnlyCollection<ChatSession> GetAllSessions();
    ChatSession GetSession(string sessionId);
    bool UpdateSessionTitle(string sessionId, string title);
    void SetCurrentSession(string sessionId);
    string GetCurrentSessionId();
    IReadOnlyList<ChatHistoryItem> GetSessionHistory(string sessionId);
    void DeleteSession(string sessionId);

    // Search
    ChatSearchResult SearchMessages(string keyword, string sessionId = null);
    ChatSearchResult SearchByDateRange(DateTime startDate, DateTime endDate, string sessionId = null);

    // Analytics
    ChatAnalytics GetAnalytics(string sessionId = null);

    // Session Pinning
    bool PinSession(string sessionId);
    bool UnpinSession(string sessionId);
    bool IsSessionPinned(string sessionId);

    // Session Tags
    bool AddTagToSession(string sessionId, string tag);
    bool RemoveTagFromSession(string sessionId, string tag);
    IReadOnlyList<ChatSession> GetSessionsByTag(string tag);
}
