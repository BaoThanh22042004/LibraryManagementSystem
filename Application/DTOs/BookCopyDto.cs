using Domain.Enums;

namespace Application.DTOs;

public class BookCopyDto
{
    public int Id { get; set; }
    public string CopyNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
}

public class CreateBookCopyDto
{
    public int BookId { get; set; }
    public string CopyNumber { get; set; } = string.Empty;
}

public class UpdateBookCopyDto
{
    public string CopyNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
}