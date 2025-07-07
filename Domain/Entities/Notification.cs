using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a notification sent to a user or system-wide.
/// Supports UC030–UC036 (notification creation, update, viewing, and status management).
/// Business Rules: BR-20 (notification requirement), BR-21 (notification format).
/// </summary>
public class Notification : BaseEntity
{
	/// <summary>
	/// The ID of the user who receives the notification (nullable for system-wide notifications).
	/// </summary>
	public int? UserId { get; set; }

	/// <summary>
	/// The type of notification (e.g., overdue, reservation, system message).
	/// </summary>
	public NotificationType Type { get; set; }

	/// <summary>
	/// The subject of the notification (max 200 characters).
	/// </summary>
	[MaxLength(200)]
	public string Subject { get; set; } = string.Empty;

	/// <summary>
	/// The message content of the notification (max 500 characters).
	/// </summary>
	[MaxLength(500)]
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// The current status of the notification (pending, sent, read, etc.).
	/// </summary>
	public NotificationStatus Status { get; set; }

	/// <summary>
	/// The date and time the notification was sent, if applicable.
	/// </summary>
	public DateTime? SentAt { get; set; }

	/// <summary>
	/// The date and time the notification was read by the recipient, if applicable.
	/// </summary>
	public DateTime? ReadAt { get; set; }

	/// <summary>
	/// Navigation property to the recipient user, if applicable.
	/// </summary>
	public User? User { get; set; }
}
