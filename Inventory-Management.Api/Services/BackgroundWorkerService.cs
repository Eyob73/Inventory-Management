using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory_Management.Api.Services;

public class BackgroundWorkerService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundWorkerService> _logger;

    public BackgroundWorkerService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundWorkerService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Worker Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (tenantId, workItem) = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    if (tenantId.HasValue)
                    {
                        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                        currentTenant.SetTenant(tenantId.Value);
                    }
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background work item.");
                }
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while dequeuing a background task.");
            }
        }

        _logger.LogInformation("Background Worker Service is stopping.");
    }
}
