using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

public class CleanupBackgroundService : BackgroundService
{
    private readonly ILogger<CleanupBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly TimeSpan _cleanupInterval;

    public CleanupBackgroundService(
        ILogger<CleanupBackgroundService> logger,
        IConfiguration configuration,
        IChatHistoryService chatHistoryService)
    {
        _logger = logger;
        _configuration = configuration;
        _chatHistoryService = chatHistoryService;
        _cleanupInterval = TimeSpan.FromHours(configuration.GetValue<int>("CleanupIntervalHours", 24));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup background service started with interval: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        _logger.LogInformation("Starting cleanup at {Time}", DateTime.UtcNow);

        // Clean old sessions (older than 90 days with no activity)
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        var allSessions = _chatHistoryService.GetAllSessions();
        var sessionsToDelete = allSessions
            .Where(s => s.LastActiveAt < cutoffDate && s.Id != _chatHistoryService.GetCurrentSessionId())
            .ToList();

        foreach (var session in sessionsToDelete)
        {
            _chatHistoryService.DeleteSession(session.Id);
            _logger.LogInformation("Deleted old session: {SessionId}", session.Id);
        }

        _logger.LogInformation("Cleanup completed. Removed {Count} sessions", sessionsToDelete.Count);
    }
}