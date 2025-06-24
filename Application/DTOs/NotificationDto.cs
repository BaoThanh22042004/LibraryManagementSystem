using Domain.Enums;

namespace Application.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
}

public class CreateNotificationDto
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int? UserId { get; set; }
}

public class UpdateNotificationDto
{
    public NotificationStatus Status { get; set; }
}