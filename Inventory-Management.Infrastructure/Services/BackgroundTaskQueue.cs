using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Inventory_Management.Application.Interfaces.Services;

namespace Inventory_Management.Infrastructure.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<(Guid? TenantId, Func<IServiceProvider, CancellationToken, ValueTask> WorkItem)> _queue;

    public BackgroundTaskQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<(Guid? TenantId, Func<IServiceProvider, CancellationToken, ValueTask> WorkItem)>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Guid? tenantId, Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
            throw new ArgumentNullException(nameof(workItem));

        await _queue.Writer.WriteAsync((tenantId, workItem));
    }

    public async ValueTask<(Guid? TenantId, Func<IServiceProvider, CancellationToken, ValueTask> WorkItem)> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
