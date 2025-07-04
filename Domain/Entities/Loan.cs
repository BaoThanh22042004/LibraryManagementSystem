using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a loan (borrowing transaction) of a book copy by a member.
/// Supports UC018–UC021 (checkout, return, renewal, and history).
/// Business Rules: BR-13 (loan limit), BR-14 (standard loan period), BR-16 (loan eligibility).
/// </summary>
public class Loan : BaseEntity
{
	/// <summary>
	/// The ID of the member who borrowed the book copy.
	/// </summary>
	public int MemberId { get; set; }

	/// <summary>
	/// The ID of the borrowed book copy.
	/// </summary>
	public int BookCopyId { get; set; }

	/// <summary>
	/// The date the loan was created (checkout date).
	/// </summary>
	public DateTime LoanDate { get; set; }

	/// <summary>
	/// The due date for returning the book copy.
	/// </summary>
	public DateTime DueDate { get; set; }

	/// <summary>
	/// The date the book copy was returned, if applicable.
	/// </summary>
	public DateTime? ReturnDate { get; set; }

	/// <summary>
	/// The current status of the loan (Active, Returned, Overdue, Lost).
	/// </summary>
	public LoanStatus Status { get; set; }

	// Navigation properties
	/// <summary>
	/// Navigation property to the borrowing member.
	/// </summary>
	public Member Member { get; set; } = null!;
	/// <summary>
	/// Navigation property to the borrowed book copy.
	/// </summary>
	public BookCopy BookCopy { get; set; } = null!;
	/// <summary>
	/// Collection of fines associated with this loan.
	/// </summary>
	public ICollection<Fine> Fines { get; set; } = [];
}
