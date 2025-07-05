using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for creating a new book
/// Supports UC010 (Add Book)
/// </summary>
public class CreateBookDto
{
    /// <summary>
    /// The title of the book (required)
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The author of the book (required)
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// The ISBN of the book (required, unique)
    /// </summary>
    public string ISBN { get; set; } = string.Empty;
    
    /// <summary>
    /// The publisher of the book (optional)
    /// </summary>
    public string? Publisher { get; set; }
    
    /// <summary>
    /// A description of the book content (optional)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL or path to the book's cover image (optional)
    /// </summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// The publication date of the book (optional)
    /// </summary>
    public DateTime? PublicationDate { get; set; }
    
    /// <summary>
    /// IDs of categories this book belongs to
    /// </summary>
    public List<int> CategoryIds { get; set; } = new List<int>();
    
    /// <summary>
    /// Number of initial copies to create (default: 1)
    /// </summary>
    public int InitialCopies { get; set; } = 1;
}

/// <summary>
/// Data transfer object for updating an existing book
/// Supports UC011 (Update Book)
/// </summary>
public class UpdateBookDto
{
    /// <summary>
    /// The ID of the book to update
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The updated title of the book (required)
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The updated author of the book (required)
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// The updated ISBN of the book (required, unique)
    /// </summary>
    public string ISBN { get; set; } = string.Empty;
    
    /// <summary>
    /// The updated publisher of the book (optional)
    /// </summary>
    public string? Publisher { get; set; }
    
    /// <summary>
    /// The updated description of the book content (optional)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Updated URL or path to the book's cover image (optional)
    /// </summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// The updated publication date of the book (optional)
    /// </summary>
    public DateTime? PublicationDate { get; set; }
    
    /// <summary>
    /// Updated IDs of categories this book belongs to
    /// </summary>
    public List<int> CategoryIds { get; set; } = new List<int>();
    
    /// <summary>
    /// The updated status of the book
    /// </summary>
    public BookStatus Status { get; set; }
}

/// <summary>
/// Basic book information DTO for list views
/// Supports UC013 (Search Books), UC014 (Browse by Category)
/// </summary>
public class BookBasicDto
{
    /// <summary>
    /// The unique identifier of the book
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The title of the book
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The author of the book
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// The ISBN of the book
    /// </summary>
    public string ISBN { get; set; } = string.Empty;
    
    /// <summary>
    /// URL or path to the book's cover image
    /// </summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// The current status of the book
    /// </summary>
    public BookStatus Status { get; set; }
    
    /// <summary>
    /// Total number of copies for this book
    /// </summary>
    public int TotalCopies { get; set; }
    
    /// <summary>
    /// Number of available copies for this book
    /// </summary>
    public int AvailableCopies { get; set; }
}

/// <summary>
/// Detailed book information DTO
/// Supports UC013 (Search Books) for detailed view
/// </summary>
public class BookDetailDto : BookBasicDto
{
    /// <summary>
    /// The publisher of the book
    /// </summary>
    public string? Publisher { get; set; }
    
    /// <summary>
    /// A description of the book content
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The publication date of the book
    /// </summary>
    public DateTime? PublicationDate { get; set; }
    
    /// <summary>
    /// Creation date of the book
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last update date of the book
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Categories this book belongs to
    /// </summary>
    public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    
    /// <summary>
    /// Copies of this book
    /// </summary>
    public List<BookCopyBasicDto> Copies { get; set; } = new List<BookCopyBasicDto>();
}

/// <summary>
/// Data transfer object for providing a paginated list of books
/// Supports UC013 (Search Books), UC014 (Browse by Category)
/// </summary>
public class PaginatedBooksDto : PagedResult<BookBasicDto>;

/// <summary>
/// Data transfer object for providing book search parameters
/// Supports UC013 (Search Books)
/// </summary>
public class BookSearchParametersDto
{
    /// <summary>
    /// Optional search term to filter books by title, author, or ISBN
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Optional category ID to filter books by category
    /// </summary>
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// Page number for pagination (default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Page size for pagination (default: 10)
    /// </summary>
    public int PageSize { get; set; } = 10;
}