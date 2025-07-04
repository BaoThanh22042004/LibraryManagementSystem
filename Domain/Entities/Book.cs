using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a book in the library catalog.
/// This entity supports UC010 (Add Book), UC011 (Update Book), UC012 (Delete Book),
/// UC013 (Search Books), and UC014 (Browse by Category).
/// </summary>
/// <remarks>
/// Key Features:
/// - Supports multiple categories per book (many-to-many relationship)
/// - Contains multiple physical copies (one-to-many relationship with BookCopy)
/// - Tracks publication information and cover images
/// - Enforces ISBN uniqueness across the catalog
/// - Supports status tracking (Available, Unavailable, UnderMaintenance)
/// 
/// Business Rules:
/// - BR-06: Only Librarian or Admin can add, edit, or delete books
/// - BR-07: Books with active loans or reservations cannot be deleted
/// - BR-22: All book operations must be logged for audit purposes
/// </remarks>
public class Book : BaseEntity
{
	/// <summary>
	/// The title of the book. Required field for all books.
	/// Used in UC013 (Search Books) for title-based searching.
	/// </summary>
	public string Title { get; set; } = string.Empty;
	
	/// <summary>
	/// The author of the book. Required field for all books.
	/// Used in UC013 (Search Books) for author-based searching.
	/// </summary>
	public string Author { get; set; } = string.Empty;
	
	/// <summary>
	/// The International Standard Book Number. Must be unique across the catalog.
	/// Used in UC013 (Search Books) for ISBN-based searching.
	/// Maximum length: 20 characters.
	/// </summary>
	[MaxLength(20)]
	public string ISBN { get; set; } = string.Empty;
	
	/// <summary>
	/// The publisher of the book. Optional field.
	/// </summary>
	public string? Publisher { get; set; }
	
	/// <summary>
	/// A description of the book content. Optional field.
	/// </summary>
	public string? Description { get; set; }
	
	/// <summary>
	/// URL or path to the book's cover image. Optional field.
	/// Used in UC010 and UC011 for cover image management.
	/// </summary>
	public string? CoverImageUrl { get; set; }
	
	/// <summary>
	/// The publication date of the book. Optional field.
	/// </summary>
	public DateTime? PublicationDate { get; set; }
	
	/// <summary>
	/// The current status of the book in the catalog.
	/// Used in UC011 (Update Book) for status management.
	/// </summary>
	public BookStatus Status { get; set; }

	// Navigation properties
	/// <summary>
	/// Collection of physical copies of this book.
	/// Used in UC015-UC017 for copy management.
	/// </summary>
	public ICollection<BookCopy> Copies { get; set; } = [];
	
	/// <summary>
	/// Collection of categories this book belongs to.
	/// Used in UC014 (Browse by Category) for categorization.
	/// </summary>
	public ICollection<Category> Categories { get; set; } = [];
}
