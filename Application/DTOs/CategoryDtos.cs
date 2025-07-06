using Application.Common;
using System.Text.Json.Serialization;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for creating a new category
/// Supports UC037 (Create Category)
/// </summary>
public record CreateCategoryRequest
{
    /// <summary>
    /// The unique name of the category (required, 1-100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the category (max 500 characters)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Optional URL or path to the category's cover image
    /// </summary>
    public string? CoverImageUrl { get; set; }
}

/// <summary>
/// Data transfer object for updating an existing category
/// Supports UC038 (Update Category)
/// </summary>
public record UpdateCategoryRequest
{
    /// <summary>
    /// The ID of the category to update
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The updated name of the category (required, 1-100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Updated description of the category (max 500 characters)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Updated URL or path to the category's cover image
    /// </summary>
    public string? CoverImageUrl { get; set; }
}

/// <summary>
/// Data transfer object for retrieving category details
/// Supports UC040 (View Category Details), UC041 (Browse Categories)
/// </summary>
public record CategoryDto
{
    /// <summary>
    /// The unique identifier of the category
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The name of the category
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The description of the category
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL or path to the category's cover image
    /// </summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// Creation date of the category
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last update date of the category
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for category details with associated books
/// Supports UC040 (View Category Details)
/// </summary>
public record CategoryWithBooksDto : CategoryDto
{
    /// <summary>
    /// Collection of books assigned to this category
    /// </summary>
    public ICollection<BookBasicDto> Books { get; set; } = new List<BookBasicDto>();
    
    /// <summary>
    /// Total number of books in this category
    /// </summary>
    public int BookCount => Books.Count;
}

/// <summary>
/// Data transfer object for providing category search parameters
/// Supports UC041 (Browse Categories)
/// </summary>
public record CategorySearchRequest : PagedRequest
{
    /// <summary>
    /// Optional search term to filter categories by name or description
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Flag to indicate if pagination should be applied (false for complete list)
    /// </summary>
    public bool ApplyPagination { get; set; } = true;
}