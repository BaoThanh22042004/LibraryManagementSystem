using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Notification : BaseEntity
{
	public int? UserId { get; set; }
	public NotificationType Type { get; set; }
	[MaxLength(200)]
	public string Subject { get; set; } = string.Empty;
	[MaxLength(500)]
	public string Message { get; set; } = string.Empty;
	public NotificationStatus Status { get; set; }
	public DateTime? SentAt { get; set; }

	// Navigation properties
	public User? User { get; set; }
}
