using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Book : SoftDeleteEntity
{
	public string Title { get; set; } = string.Empty;
	public string Author { get; set; } = string.Empty;
	[MaxLength(20)]
	public string ISBN { get; set; } = string.Empty;
	public string? Publisher { get; set; }
	public DateTime? PublicationDate { get; set; }
	public int? PageCount { get; set; }
	[MaxLength(2000)]
	public string? Description { get; set; }
	public string? CoverImageUrl { get; set; }
	public BookFormat Format { get; set; }
	[MaxLength(50)]
	public string? Language { get; set; } = "English";
	public int TotalCopies { get; set; }
	public int AvailableCopies { get; set; }
	public int ReservedCopies { get; set; }
	public BookStatus Status { get; set; }

	// Navigation properties
	public ICollection<BookCopy> Copies { get; set; } = [];
	public ICollection<Category> Categories { get; set; } = [];
}
