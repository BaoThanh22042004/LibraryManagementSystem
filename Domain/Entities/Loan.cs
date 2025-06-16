using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Loan : BaseEntity
{
    public int MemberId { get; set; }
    public int BookCopyId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
    public int RenewalCount { get; set; }
	public decimal? OverdueFine { get; set; }
	[NotMapped]
	public bool IsOverdue => ReturnDate == null && DueDate < DateTime.UtcNow;
	public string? Notes { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public BookCopy BookCopy { get; set; } = null!;
    public ICollection<Fine> Fines { get; set; } = [];
}
