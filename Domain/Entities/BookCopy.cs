using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class BookCopy : SoftDeleteEntity
{
    public int BookId { get; set; }
    [MaxLength(50)]
	public string CopyNumber { get; set; } = string.Empty;
    [MaxLength(100)]
	public string? Barcode { get; set; }
    public CopyCondition Condition { get; set; }
    public CopyStatus Status { get; set; }
	[MaxLength(100)]
	public string? Location { get; set; }
    public DateTime? LastInventoryDate { get; set; }
	[MaxLength(500)]
	public string? Notes { get; set; }
    
    // Navigation properties
    public Book Book { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
}
