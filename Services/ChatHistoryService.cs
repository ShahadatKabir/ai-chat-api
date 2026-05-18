using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatHistoryService : IChatHistoryService
{
    private readonly ConcurrentQueue<ChatHistoryItem> _history = new();
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private string _currentSessionId = string.Empty;

    public ChatHistoryService()
    {
        // Create default session
        var defaultSession = new ChatSession { Title = "Default Conversation" };
        _sessions.TryAdd(defaultSession.Id, defaultSession);
        _currentSessionId = defaultSession.Id;
    }

    public Task AddAsync(ChatHistoryItem item)
    {
        if (string.IsNullOrEmpty(item.SessionId))
        {
            item.SessionId = _currentSessionId;
        }

        _history.Enqueue(item);
        
        // Update session last active time and message count
        if (_sessions.TryGetValue(item.SessionId, out var session))
        {
            session.LastActiveAt = DateTime.UtcNow;
            session.MessageCount++;
        }

        // Keep history limit
        while (_history.Count > 500)
        {
            _history.TryDequeue(out _);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<ChatHistoryItem> GetHistory()
    {
        return _history.ToArray();
    }

    public PagedChatHistoryResult GetHistoryPage(int page, int pageSize, string sessionId = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _history.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query = query.Where(h => h.SessionId == sessionId);
        }

        var items = query
            .OrderByDescending(h => h.Timestamp)
            .ToList();

        var totalItems = items.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedChatHistoryResult
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
        };
    }

    public bool DeleteMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return false;
        }

        var existingItems = _history.ToList();
        var itemToRemove = existingItems.FirstOrDefault(h => h.Id == messageId);
        if (itemToRemove == null)
        {
            return false;
        }

        var remainingItems = existingItems.Where(h => h.Id != messageId).ToList();
        while (_history.TryDequeue(out _)) { }
        foreach (var item in remainingItems)
        {
            _history.Enqueue(item);
        }

        if (_sessions.TryGetValue(itemToRemove.SessionId, out var session) && session.MessageCount > 0)
        {
            session.MessageCount--;
            session.LastActiveAt = DateTime.UtcNow;
        }

        return true;
    }

    public void Clear()
    {
        while (_history.TryDequeue(out _))
        {
        }
    }

    // Session Management
    public ChatSession CreateSession(string title)
    {
        var session = new ChatSession { Title = title };
        _sessions.TryAdd(session.Id, session);
        return session;
    }

    public IReadOnlyCollection<ChatSession> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    public ChatSession GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public bool UpdateSessionTitle(string sessionId, string title)
    {
        if (string.IsNullOrWhiteSpace(title) || !_sessions.TryGetValue(sessionId, out var session))
        {
            return false;
        }

        session.Title = title.Trim();
        session.LastActiveAt = DateTime.UtcNow;
        return true;
    }

    public void SetCurrentSession(string sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            _currentSessionId = sessionId;
        }
    }

    public string GetCurrentSessionId()
    {
        return _currentSessionId;
    }

    public IReadOnlyList<ChatHistoryItem> GetSessionHistory(string sessionId)
    {
        return _history.Where(h => h.SessionId == sessionId).ToList();
    }

    public void DeleteSession(string sessionId)
    {
        if (sessionId != _currentSessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            var itemsToRemove = _history.Where(h => h.SessionId == sessionId).ToList();
            // Note: ConcurrentQueue doesn't support removing items, so we'll need to rebuild it
            var tempList = _history.Where(h => h.SessionId != sessionId).ToList();
            while (_history.TryDequeue(out _)) { }
            foreach (var item in tempList)
            {
                _history.Enqueue(item);
            }
        }
    }

    // Search functionality
    public ChatSearchResult SearchMessages(string keyword, string sessionId = null)
    {
        var query = _history.AsEnumerable();

        if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(h => h.SessionId == sessionId);
        }

        var results = query.Where(h =>
            h.UserMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            h.BotResponse.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        return new ChatSearchResult
        {
            TotalMatches = results.Count,
            Results = results
        };
    }

    public ChatSearchResult SearchByDateRange(DateTime startDate, DateTime endDate, string sessionId = null)
    {
        var query = _history.AsEnumerable();

        if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(h => h.SessionId == sessionId);
        }

        var results = query.Where(h =>
            h.Timestamp >= startDate && h.Timestamp <= endDate
        ).ToList();

        return new ChatSearchResult
        {
            TotalMatches = results.Count,
            Results = results
        };
    }

    // Analytics
    public ChatAnalytics GetAnalytics(string sessionId = null)
    {
        var query = _history.AsEnumerable();

        if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(h => h.SessionId == sessionId);
        }

        var items = query.ToList();

        if (!items.Any())
        {
            return new ChatAnalytics();
        }

        var modelUsage = items
            .GroupBy(i => i.Model)
            .ToDictionary(g => g.Key, g => g.Count());

        var mostUsedModel = modelUsage.OrderByDescending(m => m.Value).FirstOrDefault().Key ?? "N/A";

        return new ChatAnalytics
        {
            TotalMessages = items.Count,
            TotalSessions = string.IsNullOrEmpty(sessionId) ? _sessions.Count : 1,
            AverageMessageLength = items.Average(i => i.UserMessage.Length),
            AverageResponseLength = items.Average(i => i.BotResponse.Length),
            ModelUsageCount = modelUsage,
            EarliestMessage = items.Min(i => i.Timestamp),
            LatestMessage = items.Max(i => i.Timestamp),
            MostUsedModel = mostUsedModel
        };
    }

    // ---- Session Pinning ----
    public bool PinSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsPinned = true;
            session.LastActiveAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public bool UnpinSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsPinned = false;
            session.LastActiveAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public bool IsSessionPinned(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session?.IsPinned ?? false;
    }

    // ---- Session Tags ----
    public bool AddTagToSession(string sessionId, string tag)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && !string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLowerInvariant();
            if (!session.Tags.Contains(normalizedTag))
            {
                session.Tags.Add(normalizedTag);
                session.LastActiveAt = DateTime.UtcNow;
                return true;
            }
        }
        return false;
    }

    public bool RemoveTagFromSession(string sessionId, string tag)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && !string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLowerInvariant();
            var removed = session.Tags.Remove(normalizedTag);
            if (removed)
            {
                session.LastActiveAt = DateTime.UtcNow;
            }
            return removed;
        }
        return false;
    }

    public IReadOnlyList<ChatSession> GetSessionsByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return new List<ChatSession>();
        }
        var normalizedTag = tag.Trim().ToLowerInvariant();
        return _sessions.Values.Where(s => s.Tags.Contains(normalizedTag)).ToList();
    }
}
