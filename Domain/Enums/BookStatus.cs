namespace Domain.Enums;

/// <summary>
/// Represents the status of a book in the library catalog.
/// Used in book management (UC010–UC014).
/// </summary>
public enum BookStatus
{
	/// <summary>
	/// The book is available for borrowing and visible in the catalog.
	/// </summary>
	Available = 1,
	/// <summary>
	/// The book is not available for borrowing (e.g., removed, archived).
	/// </summary>
	Unavailable = 2,
	/// <summary>
	/// The book is under maintenance (e.g., being repaired).
	/// </summary>
	UnderMaintenance = 3
}
