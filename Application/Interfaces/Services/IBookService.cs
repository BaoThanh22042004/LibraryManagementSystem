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
    Task<PagedResult<BookDto>> GetPaginatedBooksByCategoryAsync(int categoryId, PagedRequest request);
    Task<PagedResult<BookDto>> SearchBooksAsync(
        string? searchTerm = null, 
        string? title = null, 
        string? author = null, 
        string? isbn = null,
        int? categoryId = null,
        PagedRequest? request = null);
    Task<List<BookDto>> GetRecentBooksAsync(int count = 10);
    Task<List<BookDto>> GetPopularBooksAsync(int count = 10);
}