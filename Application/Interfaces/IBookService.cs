using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for book management operations
/// Supports use cases UC010-UC014
/// </summary>
public interface IBookService
{
    Task<Result<BookDetailDto>> CreateBookAsync(CreateBookRequest request);
    
    Task<Result<BookDetailDto>> UpdateBookAsync(UpdateBookRequest request);
    
    Task<Result<bool>> DeleteBookAsync(int id);
    
    Task<Result<BookDetailDto>> GetBookByIdAsync(int id);
    
    Task<Result<PagedResult<BookBasicDto>>> SearchBooksAsync(BookSearchRequest request);
    
    Task<Result<bool>> IsbnExistsAsync(string isbn, int? excludeId = null);
    
    Task<Result<bool>> BookHasActiveLoansAsync(int id);
    
    Task<Result<bool>> BookHasActiveReservationsAsync(int id);
}