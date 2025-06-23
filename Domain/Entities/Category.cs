using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Category : BaseEntity
{
	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;
	[MaxLength(500)]
	public string? Description { get; set; }

	// Navigation properties
	public ICollection<Book> Books { get; set; } = [];
}
