namespace Domain.Enums;

/// <summary>
/// Represents the status of a book reservation in the system.
/// Used in reservation management (UC022–UC025).
/// </summary>
public enum ReservationStatus
{
	/// <summary>
	/// The reservation is active and waiting to be fulfilled.
	/// </summary>
	Active = 1,
	/// <summary>
	/// The reservation has been fulfilled (book is available for pickup).
	/// </summary>
	Fulfilled = 2,
	/// <summary>
	/// The reservation was cancelled by the user or staff.
	/// </summary>
	Cancelled = 3,
	/// <summary>
	/// The reservation expired before being fulfilled.
	/// </summary>
	Expired = 4
}
