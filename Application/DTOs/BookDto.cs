using Domain.Enums;

namespace Application.DTOs;

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

public class BookDetailsDto : BookDto
{
    public List<BookCopyDto> Copies { get; set; } = [];
    public List<CategoryDto> CategoryDetails { get; set; } = [];
}

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