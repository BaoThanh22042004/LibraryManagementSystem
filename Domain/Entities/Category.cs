using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Category : SoftDeleteEntity
{
    [MaxLength(100)]
	public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
	public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = [];
    public ICollection<Book> Books { get; set; } = [];
}
