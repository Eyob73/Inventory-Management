using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Guid? tenantId, Func<IServiceProvider, CancellationToken, ValueTask> workItem);
    ValueTask<(Guid? TenantId, Func<IServiceProvider, CancellationToken, ValueTask> WorkItem)> DequeueAsync(CancellationToken cancellationToken);
}
