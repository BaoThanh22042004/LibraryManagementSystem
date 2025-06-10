using Domain.Enums;

namespace Domain.Entities;

public class BookCopy : BaseEntity
{
    public int BookId { get; set; }
    public string CopyNumber { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public CopyCondition Condition { get; set; }
    public CopyStatus Status { get; set; }
    public string? Location { get; set; }
    public DateTime? LastInventoryDate { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public Book Book { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
