using System;
using System.Collections.Concurrent;

public class ClientRequestInfo
{
    private readonly ConcurrentQueue<DateTime> _requests = new();

    public void RecordRequest()
    {
        _requests.Enqueue(DateTime.UtcNow);
    }

    public void Cleanup()
    {
        while (_requests.TryPeek(out var oldest) && (DateTime.UtcNow - oldest).TotalMinutes > 1)
        {
            _requests.TryDequeue(out _);
        }
    }

    public bool IsRateLimited(int maxRequestsPerMinute)
    {
        Cleanup();
        return _requests.Count >= maxRequestsPerMinute;
    }

    public int GetCount()
    {
        Cleanup();
        return _requests.Count;
    }
}
