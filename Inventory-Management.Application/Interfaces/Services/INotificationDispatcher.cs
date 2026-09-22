using Inventory_Management.Application.DTOs.Notification;

namespace Inventory_Management.Application.Interfaces.Services;

public interface INotificationDispatcher
{
    Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
