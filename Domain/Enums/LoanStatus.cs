namespace Domain.Enums;

/// <summary>
/// Represents the status of a loan (borrowing transaction).
/// Used in loan management (UC018–UC021).
/// </summary>
public enum LoanStatus
{
	/// <summary>
	/// The loan is active (book is borrowed and not yet returned).
	/// </summary>
	Active = 1,
	/// <summary>
	/// The loan has been completed and the book returned.
	/// </summary>
	Returned = 2,
	/// <summary>
	/// The loan is overdue (book not returned by due date).
	/// </summary>
	Overdue = 3,
	/// <summary>
	/// The book is lost and not returned.
	/// </summary>
	Lost = 4
}
