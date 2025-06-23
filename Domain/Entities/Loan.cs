using Domain.Enums;

namespace Domain.Entities;

public class Loan : BaseEntity
{
	public int MemberId { get; set; }
	public int BookCopyId { get; set; }
	public DateTime LoanDate { get; set; }
	public DateTime DueDate { get; set; }
	public DateTime? ReturnDate { get; set; }
	public LoanStatus Status { get; set; }

	// Navigation properties
	public Member Member { get; set; } = null!;
	public BookCopy BookCopy { get; set; } = null!;
	public ICollection<Fine> Fines { get; set; } = [];
}
