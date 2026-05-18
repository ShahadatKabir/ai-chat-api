using System;

public class ApiRateLimitInfo
{
    public int RemainingRequests { get; set; }
    public int MaxRequestsPerMinute { get; set; }
    public DateTime? ResetAt { get; set; }
    public bool IsLimited { get; set; }
}
