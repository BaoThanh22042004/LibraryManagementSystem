using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a physical copy of a book in the library inventory.
/// This entity supports UC015 (Add Copy), UC016 (Update Copy Status), and UC017 (Remove Copy).
/// </summary>
/// <remarks>
/// Key Features:
/// - Each copy has a unique copy number within its parent book
/// - Tracks the current status of the physical copy
/// - Links to loan and reservation history
/// - Supports status transitions based on business rules
/// 
/// Business Rules:
/// - BR-08: Copies with active loans or reservations cannot be deleted
/// - BR-09: Copy statuses include: Available, On Loan, Reserved, Lost, Damaged
/// - BR-10: A copy cannot be marked as Available unless it has been properly returned
/// - BR-22: All copy operations must be logged for audit purposes
/// </remarks>
public class BookCopy : BaseEntity
{
	/// <summary>
	/// The ID of the parent book this copy belongs to.
	/// Required foreign key relationship.
	/// </summary>
	public int BookId { get; set; }
	
	/// <summary>
	/// The unique copy number within the parent book.
	/// Format typically follows ISBN-XXX pattern (e.g., "978-123-456-001").
	/// Maximum length: 50 characters.
	/// Used in UC015 for copy identification and UC016-UC017 for copy management.
	/// </summary>
	[MaxLength(50)]
	public string CopyNumber { get; set; } = string.Empty;
	
	/// <summary>
	/// The current status of this physical copy.
	/// Used in UC016 (Update Copy Status) for status management.
	/// Affects availability for borrowing and reservations.
	/// </summary>
	public CopyStatus Status { get; set; }

	// Navigation properties
	/// <summary>
	/// The parent book this copy belongs to.
	/// Used for book information retrieval in copy operations.
	/// </summary>
	public Book Book { get; set; } = null!;
	
	/// <summary>
	/// Collection of loans associated with this copy.
	/// Used to validate copy deletion and status changes.
	/// </summary>
	public ICollection<Loan> Loans { get; set; } = [];
	
	/// <summary>
	/// Collection of reservations associated with this copy.
	/// Used to validate copy deletion and status changes.
	/// </summary>
	public ICollection<Reservation> Reservations { get; set; } = [];
}
