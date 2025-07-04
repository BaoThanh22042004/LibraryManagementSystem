using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for book copy information used across multiple use cases.
/// Contains copy details with associated book information for display purposes.
/// </summary>
public class BookCopyDto
{
    public int Id { get; set; }
    public string CopyNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public BookDto Book { get; set; } = null!;
}

/// <summary>
/// Data transfer object for creating new book copies (UC015 - Add Copy).
/// Supports both auto-generated and custom copy numbers.
/// </summary>
public class CreateBookCopyDto
{
    public int BookId { get; set; }
    public string CopyNumber { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating book copy information (UC016 - Update Copy Status).
/// Primarily used for status changes and copy number updates.
/// </summary>
public class UpdateBookCopyDto
{
    public string CopyNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
}