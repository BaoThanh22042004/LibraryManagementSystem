using Domain.Enums;

namespace Domain.Entities;

public class Fine : BaseEntity
{
    public int MemberId { get; set; }
    public int? LoanId { get; set; }
    public FineType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime FineDate { get; set; }
    public DateTime? DueDate { get; set; }
    public FineStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? WaiverReason { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public Loan? Loan { get; set; }
}
