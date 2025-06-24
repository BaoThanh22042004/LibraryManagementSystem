using Domain.Enums;

namespace Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int BooksCount { get; set; }
}

public class CategoryDetailsDto : CategoryDto
{
    public List<BookDto> Books { get; set; } = [];
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
}