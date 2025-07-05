using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for book management operations
/// Supports use cases UC010-UC014
/// </summary>
public interface IBookService
{
    /// <summary>
    /// Creates a new book with initial copies
    /// Supports UC010 (Add Book)
    /// </summary>
    /// <param name="createBookDto">The book data to create</param>
    /// <returns>Result containing the created book data with ID</returns>
    Task<Result<BookDetailDto>> CreateBookAsync(CreateBookDto createBookDto);
    
    /// <summary>
    /// Updates an existing book
    /// Supports UC011 (Update Book)
    /// </summary>
    /// <param name="updateBookDto">The updated book data</param>
    /// <returns>The updated book data</returns>
    Task<Result<BookDetailDto>> UpdateBookAsync(UpdateBookDto updateBookDto);
    
    /// <summary>
    /// Deletes a book by ID
    /// Supports UC012 (Delete Book)
    /// </summary>
    /// <param name="id">The ID of the book to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<Result<bool>> DeleteBookAsync(int id);
    
    /// <summary>
    /// Gets a book by ID with detailed information
    /// Supports UC013 (Search Books) - detailed view
    /// </summary>
    /// <param name="id">The ID of the book to retrieve</param>
    /// <returns>The detailed book information</returns>
    Task<Result<BookDetailDto>> GetBookByIdAsync(int id);
    
    /// <summary>
    /// Gets a paginated list of books based on search parameters
    /// Supports UC013 (Search Books) and UC014 (Browse by Category)
    /// </summary>
    /// <param name="searchParams">The search and pagination parameters</param>
    /// <returns>Result containing the paginated list of books</returns>
    Task<Result<PaginatedBooksDto>> SearchBooksAsync(BookSearchParametersDto searchParams);
    
    /// <summary>
    /// Checks if a book with the specified ISBN already exists
    /// Supports validation for UC010 (Add Book) and UC011 (Update Book)
    /// </summary>
    /// <param name="isbn">ISBN to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>Result indicating if the ISBN exists</returns>
    Task<Result<bool>> IsbnExistsAsync(string isbn, int? excludeId = null);
    
    /// <summary>
    /// Checks if a book has any active loans
    /// Supports validation for UC012 (Delete Book)
    /// </summary>
    /// <param name="id">The ID of the book to check</param>
    /// <returns>Result indicating if book has active loans</returns>
    Task<Result<bool>> BookHasActiveLoansAsync(int id);
    
    /// <summary>
    /// Checks if a book has any active reservations
    /// Supports validation for UC012 (Delete Book)
    /// </summary>
    /// <param name="id">The ID of the book to check</param>
    /// <returns>Result indicating if book has active reservations</returns>
    Task<Result<bool>> BookHasActiveReservationsAsync(int id);
}