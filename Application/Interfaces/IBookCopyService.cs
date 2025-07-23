using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for book copy management operations
/// Supports use cases UC015-UC017
/// </summary>
public interface IBookCopyService
{
    Task<Result<BookCopyDetailDto>> CreateBookCopyAsync(CreateBookCopyRequest request);
    
    Task<Result<IEnumerable<int>>> CreateMultipleBookCopiesAsync(CreateMultipleBookCopiesRequest request);
    
    Task<Result<BookCopyDetailDto>> UpdateBookCopyStatusAsync(UpdateBookCopyStatusRequest request);
    
    Task<Result<bool>> DeleteBookCopyAsync(int id);
    
    Task<Result<BookCopyDetailDto>> GetBookCopyByIdAsync(int id);
    
    Task<Result<IEnumerable<BookCopyBasicDto>>> GetCopiesByBookIdAsync(int bookId);

    Task<Result<BookCopyDetailDto>> GetBookCopyByCopyNumberAsync(string bookCopyNumber);

    Task<Result<string>> GenerateUniqueCopyNumberAsync(int bookId);
    
    Task<Result<bool>> CopyNumberExistsAsync(int bookId, string copyNumber);
    
    Task<Result<bool>> CopyHasActiveLoansAsync(int id);
    
    Task<Result<bool>> CopyHasActiveReservationsAsync(int id);
}