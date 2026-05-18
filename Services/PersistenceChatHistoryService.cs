using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class PersistenceChatHistoryService : IChatHistoryService
{
    private readonly IDataPersistenceService _persistence;
    private readonly ConcurrentQueue<ChatHistoryItem> _history = new();
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private string _currentSessionId = string.Empty;
    private readonly Timer _saveTimer;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public PersistenceChatHistoryService(IDataPersistenceService persistence)
    {
        _persistence = persistence;
        _ = LoadHistoryAsync();
        _ = LoadSessionsAsync();

        // Auto-save every 30 seconds
        _saveTimer = new Timer(async _ => await SaveAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _persistence.LoadAsync<List<ChatHistoryItem>>("history");
        if (history != null)
        {
            foreach (var item in history)
            {
                _history.Enqueue(item);
            }
        }
    }

    private async Task LoadSessionsAsync()
    {
        var sessions = await _persistence.LoadAsync<List<ChatSession>>("sessions");
        if (sessions != null)
        {
            foreach (var session in sessions)
            {
                _sessions.TryAdd(session.Id, session);
            }
        }

        if (!_sessions.Any())
        {
            var defaultSession = new ChatSession { Title = "Default Conversation" };
            _sessions.TryAdd(defaultSession.Id, defaultSession);
            _currentSessionId = defaultSession.Id;
        }
    }

    private async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            await _persistence.SaveAsync("history", _history.ToList());
            await _persistence.SaveAsync("sessions", _sessions.Values.ToList());
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public Task AddAsync(ChatHistoryItem item)
    {
        if (string.IsNullOrEmpty(item.SessionId))
        {
            item.SessionId = _currentSessionId;
        }

        _history.Enqueue(item);

        if (_sessions.TryGetValue(item.SessionId, out var session))
        {
            session.LastActiveAt = DateTime.UtcNow;
            session.MessageCount++;
        }

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

        var items = query.OrderByDescending(h => h.Timestamp).ToList();

        var totalItems = items.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedChatHistoryResult
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            Items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList()
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
        while (_history.TryDequeue(out _)) { }
        _sessions.Clear();
    }

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
            var tempList = _history.Where(h => h.SessionId != sessionId).ToList();
            while (_history.TryDequeue(out _)) { }
            foreach (var item in tempList)
            {
                _history.Enqueue(item);
            }
        }
    }

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
            AverageMessageLength = items.Average(i => i.UserMessage?.Length ?? 0),
            AverageResponseLength = items.Average(i => i.BotResponse?.Length ?? 0),
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