namespace Inventory_Management.Application.DTOs;

public class PushSubscriptionKeys
{
    public string p256dh { get; set; } = string.Empty;
    public string auth { get; set; } = string.Empty;
}

public class PushSubscriptionDto
{
    public string endpoint { get; set; } = string.Empty;
    public PushSubscriptionKeys keys { get; set; } = new();
}
