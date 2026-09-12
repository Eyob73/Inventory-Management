using Inventory_Management.Api.Hubs;
using Inventory_Management.Application.DTOs.Notification;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Inventory_Management.Api.Services;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.User(userId).ReceiveNotification(notification);
    }
}
