using Domain.Enums;

namespace Domain.Entities;

public class Book : SoftDeleteEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public int? PageCount { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public BookFormat Format { get; set; }
    public string? Language { get; set; } = "English";
    public decimal? Rating { get; set; }
    public int RatingCount { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int ReservedCopies { get; set; }
    public BookStatus Status { get; set; }
    
    // Navigation properties
    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
