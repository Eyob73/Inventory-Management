using Inventory_Management.Application.DTOs.Notification;

namespace Inventory_Management.Api.Hubs;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}
