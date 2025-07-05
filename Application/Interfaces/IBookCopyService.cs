using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for book copy management operations
/// Supports use cases UC015-UC017
/// </summary>
public interface IBookCopyService
{
    /// <summary>
    /// Creates a new book copy
    /// Supports UC015 (Add Copy)
    /// </summary>
    /// <param name="createBookCopyDto">The copy data to create</param>
    /// <returns>The created copy data with ID</returns>
    Task<Result<BookCopyDetailDto>> CreateBookCopyAsync(CreateBookCopyDto createBookCopyDto);
    
    /// <summary>
    /// Creates multiple book copies at once
    /// Supports UC015 (Add Copy) - bulk creation alternative flow
    /// </summary>
    /// <param name="createMultipleDto">The data for creating multiple copies</param>
    /// <returns>List of created copy IDs</returns>
    Task<Result<IEnumerable<int>>> CreateMultipleBookCopiesAsync(CreateMultipleBookCopiesDto createMultipleDto);
    
    /// <summary>
    /// Updates a book copy's status
    /// Supports UC016 (Update Copy Status)
    /// </summary>
    /// <param name="updateStatusDto">The status update data</param>
    /// <returns>The updated copy data</returns>
    Task<Result<BookCopyDetailDto>> UpdateBookCopyStatusAsync(UpdateBookCopyStatusDto updateStatusDto);
    
    /// <summary>
    /// Deletes a book copy by ID
    /// Supports UC017 (Remove Copy)
    /// </summary>
    /// <param name="id">The ID of the copy to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<Result<bool>> DeleteBookCopyAsync(int id);
    
    /// <summary>
    /// Gets a book copy by ID with detailed information
    /// </summary>
    /// <param name="id">The ID of the copy to retrieve</param>
    /// <returns>The detailed copy information</returns>
    Task<Result<BookCopyDetailDto>> GetBookCopyByIdAsync(int id);
    
    /// <summary>
    /// Gets all copies for a specific book
    /// </summary>
    /// <param name="bookId">The ID of the book</param>
    /// <returns>List of book copies</returns>
    Task<Result<IEnumerable<BookCopyBasicDto>>> GetCopiesByBookIdAsync(int bookId);
    
    /// <summary>
    /// Generates a unique copy number for a book
    /// Used in UC015 (Add Copy) when auto-generating copy numbers
    /// </summary>
    /// <param name="bookId">The ID of the book</param>
    /// <returns>Result containing a unique copy number</returns>
    Task<Result<string>> GenerateUniqueCopyNumberAsync(int bookId);
    
    /// <summary>
    /// Checks if a copy number already exists for a specific book
    /// Supports validation for UC015 (Add Copy)
    /// </summary>
    /// <param name="bookId">The ID of the book</param>
    /// <param name="copyNumber">Copy number to check</param>
    /// <returns>Result containing true if copy number exists, false otherwise</returns>
    Task<Result<bool>> CopyNumberExistsAsync(int bookId, string copyNumber);
    
    /// <summary>
    /// Checks if a book copy has any active loans
    /// Supports validation for UC016 (Update Copy Status) and UC017 (Remove Copy)
    /// </summary>
    /// <param name="id">The ID of the copy to check</param>
    /// <returns>Result containing true if copy has active loans, false otherwise</returns>
    Task<Result<bool>> CopyHasActiveLoansAsync(int id);
    
    /// <summary>
    /// Checks if a book copy has any active reservations
    /// Supports validation for UC016 (Update Copy Status) and UC017 (Remove Copy)
    /// </summary>
    /// <param name="id">The ID of the copy to check</param>
    /// <returns>Result containing true if copy has active reservations, false otherwise</returns>
    Task<Result<bool>> CopyHasActiveReservationsAsync(int id);
}