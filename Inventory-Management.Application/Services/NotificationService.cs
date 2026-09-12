using Inventory_Management.Application.DTOs.Notification;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification> _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        IGenericRepository<Notification> notificationRepository,
        INotificationDispatcher dispatcher,
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task SendToUserAsync(string userId, NotificationDto dto, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            IsRead = false,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            ActionUrl = dto.Link,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = notification.Id;
        dto.Timestamp = notification.CreatedAt;
        dto.Read = false;

        await _dispatcher.SendToUserAsync(userId, dto, cancellationToken);
    }

    public async Task SendToRolesAsync(IEnumerable<string> roles, NotificationDto dto, CancellationToken cancellationToken = default)
    {
        var users = new HashSet<AppUser>();
        foreach (var role in roles)
        {
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            foreach (var u in roleUsers)
            {
                users.Add(u);
            }
        }

        foreach (var user in users)
        {
            await SendToUserAsync(user.Id, dto, cancellationToken);
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50) // Limit for performance
            .ToListAsync(cancellationToken);

        return notifications.Select(MapToDto);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification != null && notification.UserId == userId && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _notificationRepository.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _notificationRepository.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
                await _notificationRepository.UpdateAsync(notification);
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification != null && notification.UserId == userId)
        {
            await _notificationRepository.DeleteAsync(notification.Id);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        Read = n.IsRead,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType,
        Link = n.ActionUrl,
        Icon = GetIconForType(n.Type),
        Timestamp = n.CreatedAt
    };

    private static string GetIconForType(string type) => type.ToLower() switch
    {
        "warning" => "warning",
        "lowstock" => "warning",
        "outofstock" => "error",
        "info" => "info",
        "success" => "check_circle",
        "newsale" => "shopping_cart",
        "purchase" => "local_shipping",
        "product" => "inventory_2",
        "system" => "settings",
        _ => "notifications"
    };
}
