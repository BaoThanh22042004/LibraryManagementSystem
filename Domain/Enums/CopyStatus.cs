namespace Domain.Enums;

/// <summary>
/// Represents the status of a physical book copy in the library.
/// Used in copy management (UC015–UC017). Business Rules: BR-09, BR-10.
/// </summary>
public enum CopyStatus
{
	/// <summary>
	/// The copy is available for borrowing or reservation.
	/// </summary>
	Available = 1,
	/// <summary>
	/// The copy is currently borrowed by a member.
	/// </summary>
	Borrowed = 2,
	/// <summary>
	/// The copy is reserved for a member.
	/// </summary>
	Reserved = 3,
	/// <summary>
	/// The copy is damaged and not available for borrowing.
	/// </summary>
	Damaged = 4,
	/// <summary>
	/// The copy is lost and not available in the library.
	/// </summary>
	Lost = 5
}
