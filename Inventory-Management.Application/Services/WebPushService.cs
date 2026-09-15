using Inventory_Management.Application.DTOs.Notification;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WebPush;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class WebPushService : IWebPushService
{
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.PushSubscription> _subscriptionRepository;
    private readonly IConfiguration _config;
    private readonly WebPushClient _webPushClient;

    public WebPushService(IGenericRepository<Inventory_Management.Domain.Entities.PushSubscription> subscriptionRepository, IConfiguration config)
    {
        _subscriptionRepository = subscriptionRepository;
        _config = config;
        
        _webPushClient = new WebPushClient();
        
        var vapidDetails = new VapidDetails(
            _config["VapidDetails:Subject"],
            _config["VapidDetails:PublicKey"],
            _config["VapidDetails:PrivateKey"]
        );
        _webPushClient.SetVapidDetails(vapidDetails);
    }

    public async Task SendPushNotificationAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.Query().Where(s => s.UserId == userId).ToListAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = notification.Title,
                body = notification.Message,
                icon = "/icons/icon-192x192.png",
                vibrate = new[] { 100, 50, 100 },
                data = new { url = notification.Link }
            }
        });

        foreach (var sub in subscriptions)
        {
            var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            try
            {
                await _webPushClient.SendNotificationAsync(pushSubscription, payload, cancellationToken: cancellationToken);
            }
            catch (WebPushException exception)
            {
                if (exception.StatusCode == System.Net.HttpStatusCode.Gone || 
                    exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Device unsubscribed or is no longer valid, we should remove it from the database
                    await _subscriptionRepository.DeleteAsync(sub.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending WebPush: {ex.Message}");
            }
        }
    }
}
