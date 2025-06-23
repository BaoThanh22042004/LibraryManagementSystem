using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class BookCopy : BaseEntity
{
	public int BookId { get; set; }
	[MaxLength(50)]
	public string CopyNumber { get; set; } = string.Empty;
	public CopyStatus Status { get; set; }

	// Navigation properties
	public Book Book { get; set; } = null!;
	public ICollection<Loan> Loans { get; set; } = [];
	public ICollection<Reservation> Reservations { get; set; } = [];
}
