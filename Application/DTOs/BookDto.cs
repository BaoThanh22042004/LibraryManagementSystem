using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for book information used in UC013 (Search Books) and UC014 (Browse by Category).
/// Contains essential book details with calculated availability information.
/// </summary>
public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublicationDate { get; set; }
    public BookStatus Status { get; set; }
    public List<string> Categories { get; set; } = [];
    public int CopiesCount { get; set; }
    public int AvailableCopiesCount { get; set; }
}

/// <summary>
/// Extended book information for detailed views, including all copies and category details.
/// Used for comprehensive book information display.
/// </summary>
public class BookDetailsDto : BookDto
{
    public List<BookCopyDto> Copies { get; set; } = [];
    public List<CategoryDto> CategoryDetails { get; set; } = [];
}

/// <summary>
/// Data transfer object for creating new books (UC010 - Add Book).
/// Includes all required fields for book creation and initial copy generation.
/// </summary>
public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublicationDate { get; set; }
    public List<int> CategoryIds { get; set; } = [];
    public int InitialCopiesCount { get; set; } = 1;
}

/// <summary>
/// Data transfer object for updating existing books (UC011 - Update Book).
/// Supports partial updates of book information and status changes.
/// </summary>
public class UpdateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublicationDate { get; set; }
    public List<int> CategoryIds { get; set; } = [];
    public BookStatus Status { get; set; }
}