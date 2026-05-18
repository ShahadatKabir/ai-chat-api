using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

public interface IUserRateLimitService
{
    void RecordRequest(string clientId);
    ApiRateLimitInfo GetRateLimitInfo(string clientId, int maxPerMinute);
}

public class UserRateLimitService : IUserRateLimitService
{
    private readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();

    public void RecordRequest(string clientId)
    {
        var info = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());
        info.RecordRequest();
    }

    public ApiRateLimitInfo GetRateLimitInfo(string clientId, int maxPerMinute)
    {
        _clients.TryGetValue(clientId, out var info);
        var count = info != null ? info.GetCount() : 0;
        var isLimited = count >= maxPerMinute;

        return new ApiRateLimitInfo
        {
            RemainingRequests = Math.Max(0, maxPerMinute - count),
            MaxRequestsPerMinute = maxPerMinute,
            IsLimited = isLimited
        };
    }
}
