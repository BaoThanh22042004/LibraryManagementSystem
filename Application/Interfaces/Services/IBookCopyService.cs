using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IBookCopyService
{
    Task<PagedResult<BookCopyDto>> GetPaginatedBookCopiesAsync(PagedRequest request, string? searchTerm = null);
    Task<BookCopyDto?> GetBookCopyByIdAsync(int id);
    Task<List<BookCopyDto>> GetBookCopiesByBookIdAsync(int bookId);
    Task<int> CreateBookCopyAsync(CreateBookCopyDto bookCopyDto);
    Task UpdateBookCopyAsync(int id, UpdateBookCopyDto bookCopyDto);
    Task DeleteBookCopyAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> CopyNumberExistsAsync(string copyNumber, int bookId);
    Task<List<BookCopyDto>> GetAvailableBookCopiesAsync(int bookId);
    Task<bool> UpdateBookCopyStatusAsync(int id, Domain.Enums.CopyStatus status);
}