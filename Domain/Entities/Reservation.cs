using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a reservation for a book by a member.
/// Supports UC022–UC025 (reserve, cancel, fulfill, and view reservations).
/// Business Rules: BR-17 (reservation condition), BR-18 (reservation limit), BR-19 (reservation notification).
/// </summary>
public class Reservation : BaseEntity
{
	/// <summary>
	/// The ID of the member who made the reservation.
	/// </summary>
	public int MemberId { get; set; }

	/// <summary>
	/// The ID of the reserved book.
	/// </summary>
	public int BookId { get; set; }

	/// <summary>
	/// The ID of the assigned book copy, if applicable (nullable until fulfillment).
	/// </summary>
	public int? BookCopyId { get; set; }

	/// <summary>
	/// The date the reservation was made.
	/// </summary>
	public DateTime ReservationDate { get; set; }

	/// <summary>
	/// The current status of the reservation (pending, fulfilled, cancelled, etc.).
	/// </summary>
	public ReservationStatus Status { get; set; }

	// Navigation properties
	/// <summary>
	/// Navigation property to the reserving member.
	/// </summary>
	public Member Member { get; set; } = null!;
	/// <summary>
	/// Navigation property to the reserved book.
	/// </summary>
	public Book Book { get; set; } = null!;
	/// <summary>
	/// Navigation property to the assigned book copy, if applicable.
	/// </summary>
	public BookCopy? BookCopy { get; set; }
}
