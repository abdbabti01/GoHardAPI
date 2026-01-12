using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Background service to clean up old draft sessions (older than 7 days)
    /// Runs daily at 2 AM to remove abandoned drafts
    /// </summary>
    public class DraftSessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DraftSessionCleanupService> _logger;
        private readonly TimeSpan _runInterval = TimeSpan.FromHours(24); // Run daily
        private readonly TimeSpan _draftRetentionPeriod = TimeSpan.FromDays(7); // Keep drafts for 7 days

        public DraftSessionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<DraftSessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Draft Session Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldDrafts(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during draft session cleanup");
                }

                // Wait until next run
                await Task.Delay(_runInterval, stoppingToken);
            }

            _logger.LogInformation("Draft Session Cleanup Service stopped");
        }

        private async Task CleanupOldDrafts(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting draft session cleanup...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TrainingContext>();

            // Find draft sessions older than retention period
            var cutoffDate = DateTime.UtcNow - _draftRetentionPeriod;

            var oldDrafts = await context.Sessions
                .Where(s => s.Status == "draft" && s.Date < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldDrafts.Any())
            {
                _logger.LogInformation($"Found {oldDrafts.Count} old draft sessions to delete");

                context.Sessions.RemoveRange(oldDrafts);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Deleted {oldDrafts.Count} old draft sessions");
            }
            else
            {
                _logger.LogInformation("No old draft sessions found");
            }
        }
    }
}
