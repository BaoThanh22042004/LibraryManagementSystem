namespace Domain.Entities;

public class BookCategory : BaseEntity
{
    public int BookId { get; set; }
    public int CategoryId { get; set; }
    public bool IsPrimary { get; set; } // Primary category for the book
    
    // Navigation properties
    public Book Book { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
