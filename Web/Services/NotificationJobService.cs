using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Web.Services;

public class NotificationJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationJobService> _logger;

    public NotificationJobService(IServiceProvider serviceProvider, ILogger<NotificationJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // Send overdue notifications
                    var overdueResult = await notificationService.SendOverdueNotificationsAsync();
                    if (overdueResult.IsSuccess)
                    {
                        var (count, errors) = overdueResult.Value;
                        _logger.LogInformation("[NotificationJob] Overdue notifications sent: {Count}. Errors: {Errors}", count, string.Join("; ", errors));
                    }
                    else
                        _logger.LogWarning("[NotificationJob] Failed to send overdue notifications: {Error}", overdueResult.Error);

                    // Send availability notifications
                    var availResult = await notificationService.SendAvailabilityNotificationsAsync();
                    if (availResult.IsSuccess)
                    {
                        var (count, errors) = availResult.Value;
                        _logger.LogInformation("[NotificationJob] Availability notifications sent: {Count}. Errors: {Errors}", count, string.Join("; ", errors));
                    }
                    else
                        _logger.LogWarning("[NotificationJob] Failed to send availability notifications: {Error}", availResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NotificationJob] Error running notification jobs");
            }

            // Wait 24 hours before next run
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
} 