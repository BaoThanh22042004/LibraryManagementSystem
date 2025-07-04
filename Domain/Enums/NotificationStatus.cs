namespace Domain.Enums;

/// <summary>
/// Represents the status of a notification in its lifecycle.
/// Used in notification management and delivery tracking (UC030–UC036).
/// </summary>
public enum NotificationStatus
{
	/// <summary>
	/// Notification is created but not yet sent.
	/// </summary>
	Pending = 1,
	/// <summary>
	/// Notification has been sent to the recipient.
	/// </summary>
	Sent = 2,
	/// <summary>
	/// Notification failed to send.
	/// </summary>
	Failed = 3,
	/// <summary>
	/// Notification has been read by the recipient.
	/// </summary>
	Read = 4
}
