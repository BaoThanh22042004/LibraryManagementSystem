using Domain.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Fine : BaseEntity
{
    public int MemberId { get; set; }
    public int? LoanId { get; set; }
    public FineType Type { get; set; }
	public decimal Amount { get; set; }
    [DefaultValue(0)]
	public decimal AmountPaid { get; set; }
    public DateTime FineDate { get; set; }
    public DateTime? DueDate { get; set; }
    public FineStatus Status { get; set; }
    [MaxLength(500)]
	public string Description { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? WaiverReason { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public Loan? Loan { get; set; }
}
