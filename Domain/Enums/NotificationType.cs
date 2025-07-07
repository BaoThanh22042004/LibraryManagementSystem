namespace Domain.Enums;

/// <summary>
/// Represents the type of notification sent to users.
/// Used in notification management (UC030–UC036).
/// </summary>
public enum NotificationType
{
	/// <summary>
	/// Reminder for upcoming loan due dates.
	/// </summary>
	LoanReminder = 1,
	/// <summary>
	/// Notice for overdue loans.
	/// </summary>
	OverdueNotice = 2,
	/// <summary>
	/// Notification that a reserved book is now available.
	/// </summary>
	ReservationAvailable = 3,
	/// <summary>
	/// System maintenance or general system-wide notification.
	/// </summary>
	SystemMaintenance = 4,
	/// <summary>
	/// Confirmation that a reservation was successfully created.
	/// </summary>
	ReservationConfirmation = 5,
	/// <summary>
	/// Notification that a reservation was cancelled.
	/// </summary>
	ReservationCancellation = 6
}
