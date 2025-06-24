using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IBookService
{
    Task<PagedResult<BookDto>> GetPaginatedBooksAsync(PagedRequest request, string? searchTerm = null);
    Task<BookDetailsDto?> GetBookByIdAsync(int id);
    Task<BookDto?> GetBookByIsbnAsync(string isbn);
    Task<int> CreateBookAsync(CreateBookDto bookDto);
    Task UpdateBookAsync(int id, UpdateBookDto bookDto);
    Task DeleteBookAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> IsbnExistsAsync(string isbn);
    Task<List<BookDto>> GetBooksByCategoryAsync(int categoryId);
    Task<List<BookDto>> GetRecentBooksAsync(int count = 10);
    Task<List<BookDto>> GetPopularBooksAsync(int count = 10);
}