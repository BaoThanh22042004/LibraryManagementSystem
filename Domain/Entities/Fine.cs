using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Fine : BaseEntity
{
	public int MemberId { get; set; }
	public int? LoanId { get; set; }
	public FineType Type { get; set; }
	public decimal Amount { get; set; }
	public DateTime FineDate { get; set; }
	public FineStatus Status { get; set; }
	[MaxLength(500)]
	public string Description { get; set; } = string.Empty;

	// Navigation properties
	public Member Member { get; set; } = null!;
	public Loan? Loan { get; set; }
}
