using Inventory_Management.Application.DTOs.Notification;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IWebPushService
{
    Task SendPushNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
