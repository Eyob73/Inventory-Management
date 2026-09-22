using Inventory_Management.Application.DTOs.Notification;

namespace Inventory_Management.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default);
    Task SendToRolesAsync(IEnumerable<string> roles, NotificationDto notification, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default);
}
