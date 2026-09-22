namespace Inventory_Management.Application.DTOs.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Read { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? Link { get; set; }
    public string Icon { get; set; } = "notifications";
    public DateTime Timestamp { get; set; }
}
