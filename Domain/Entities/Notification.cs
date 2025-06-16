using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Notification : BaseEntity
{
    public int? UserId { get; set; }
    public NotificationType Type { get; set; }
    [MaxLength(200)]
	public string Subject { get; set; } = string.Empty;
    [MaxLength(2000)]
	public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    [MaxLength(100)]
	public string? RecipientEmail { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
}
