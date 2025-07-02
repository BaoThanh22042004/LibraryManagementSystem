using Domain.Enums;

namespace Domain.Entities;

public class Reservation : BaseEntity
{
	public int MemberId { get; set; }
	public int BookId { get; set; }
	public int? BookCopyId { get; set; }
	public DateTime ReservationDate { get; set; }
	public ReservationStatus Status { get; set; }

	// Navigation properties
	public Member Member { get; set; } = null!;
	public Book Book { get; set; } = null!;
	public BookCopy? BookCopy { get; set; }
}
