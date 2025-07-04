using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a book category for organizing and classifying books in the library system.
/// Supports UC037–UC041 (category management, browsing, and assignment).
/// Business Rules: BR-11 (category management), BR-12 (deletion restriction).
/// </summary>
public class Category : BaseEntity
{
	/// <summary>
	/// The unique name of the category. Required for identification and uniqueness.
	/// </summary>
	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Optional description of the category.
	/// </summary>
	[MaxLength(500)]
	public string? Description { get; set; }

	/// <summary>
	/// Optional URL or path to the category's cover image.
	/// </summary>
	public string? CoverImageUrl { get; set; }

	/// <summary>
	/// Collection of books assigned to this category (many-to-many relationship).
	/// </summary>
	public ICollection<Book> Books { get; set; } = [];
}
