using Domain.Enums;

namespace Domain.Entities;

public class Notification : BaseEntity
{
    public int? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? RecipientEmail { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
}
