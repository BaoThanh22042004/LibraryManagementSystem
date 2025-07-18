using System.Linq.Expressions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Implementation of the IBookService interface.
/// Provides book management functionality supporting use cases UC010-UC014.
/// </summary>
public class BookService : IBookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<BookService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new book with initial copies
    /// Implements UC010 (Add Book)
    /// </summary>
    public async Task<Result<BookDetailDto>> CreateBookAsync(CreateBookRequest request)
    {
        try
        {
            // Check if ISBN already exists
            var isbnExistsResult = await IsbnExistsAsync(request.ISBN);
            if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
            {
                _logger.LogWarning("Book creation failed: ISBN '{ISBN}' already exists", request.ISBN);
                return Result.Failure<BookDetailDto>("A book with this ISBN already exists.");
            }

            // Verify that all specified categories exist
            foreach (var categoryId in request.CategoryIds)
            {
                var categoryExists = await _unitOfWork.Repository<Category>().ExistsAsync(c => c.Id == categoryId);
                if (!categoryExists)
                {
                    _logger.LogWarning("Book creation failed: Category with ID {CategoryId} not found", categoryId);
                    return Result.Failure<BookDetailDto>($"Category with ID {categoryId} not found.");
                }
            }

            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Map DTO to entity
                var book = _mapper.Map<Book>(request);
                
                // Set initial status and timestamps
                book.Status = BookStatus.Available;
                book.CreatedAt = DateTime.UtcNow;
                
                // Add category relationships
                if (request.CategoryIds.Count != 0)
                {
                    var categories = await _unitOfWork.Repository<Category>().ListAsync(
                        c => request.CategoryIds.Contains(c.Id), asNoTracking: false);
                    
                    book.Categories = [.. categories];
                }

                // Add book to repository
                await _unitOfWork.Repository<Book>().AddAsync(book);
                
                // Save to get book ID
                await _unitOfWork.SaveChangesAsync();

                // Create initial copies
                for (int i = 0; i < request.InitialCopies; i++)
                {
                    var copyNumber = await GenerateUniqueCopyNumberAsync(book.Id, i + 1);
                    if (!copyNumber.IsSuccess)
                    {
                        _logger.LogError("Failed to generate unique copy number: {Error}", copyNumber.Error);
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result.Failure<BookDetailDto>(copyNumber.Error);
					}

					var copy = new BookCopy
                    {
                        BookId = book.Id,
                        CopyNumber = copyNumber.Value,
                        Status = CopyStatus.Available,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.Repository<BookCopy>().AddAsync(copy);
                }
                
                // Save changes with copies
                await _unitOfWork.SaveChangesAsync();
                
                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Retrieve complete book with related data for mapping
                var createdBook = await _unitOfWork.Repository<Book>().GetAsync(
                    b => b.Id == book.Id,
                    b => b.Categories,
                    b => b.Copies);

                // Map to DTO
                var bookDetailDto = _mapper.Map<BookDetailDto>(createdBook);
                
                _logger.LogInformation("Book created successfully: {BookId} - {BookTitle} with {CopyCount} copies", 
                    book.Id, book.Title, request.InitialCopies);
                
                return Result.Success(bookDetailDto);
            }
            catch
            {
                // Rollback transaction on error
                await _unitOfWork.RollbackTransactionAsync();
                throw; // Rethrow to be caught by outer try-catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book: {Message}", ex.Message);
            return Result.Failure<BookDetailDto>($"Failed to create book: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing book
    /// Implements UC011 (Update Book)
    /// </summary>
    public async Task<Result<BookDetailDto>> UpdateBookAsync(UpdateBookRequest request)
    {
        try
        {
            // Retrieve existing book with categories
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == request.Id,
                b => b.Categories,
                b => b.Copies);

            if (book == null)
            {
                _logger.LogWarning("Book update failed: Book not found with ID {BookId}", request.Id);
                return Result.Failure<BookDetailDto>($"Book with ID {request.Id} not found.");
            }

            // Check if updated ISBN already exists (excluding current book)
            if (book.ISBN != request.ISBN)
            {
                var isbnExistsResult = await IsbnExistsAsync(request.ISBN, request.Id);
                if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
                {
                    _logger.LogWarning("Book update failed: ISBN '{ISBN}' already exists", request.ISBN);
                    return Result.Failure<BookDetailDto>("A book with this ISBN already exists.");
                }
            }

            // Verify that all specified categories exist
            foreach (var categoryId in request.CategoryIds)
            {
                var categoryExists = await _unitOfWork.Repository<Category>().ExistsAsync(c => c.Id == categoryId);
                if (!categoryExists)
                {
                    _logger.LogWarning("Book update failed: Category with ID {CategoryId} not found", categoryId);
                    return Result.Failure<BookDetailDto>($"Category with ID {categoryId} not found.");
                }
            }

            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Update book properties
                book.Title = request.Title;
                book.Author = request.Author;
                book.ISBN = request.ISBN;
                book.Publisher = request.Publisher;
                book.Description = request.Description;
                book.CoverImageUrl = request.CoverImageUrl;
                book.PublicationDate = request.PublicationDate;
                book.Status = request.Status;
                book.LastModifiedAt = DateTime.UtcNow;

                // Update category relationships
                // Clear existing categories
                book.Categories.Clear();
                
                // Get and add updated categories
                var categories = await _unitOfWork.Repository<Category>().ListAsync(
                    c => request.CategoryIds.Contains(c.Id));
                
                foreach (var category in categories)
                {
                    book.Categories.Add(category);
                }

                // Update in repository
                _unitOfWork.Repository<Book>().Update(book);
                await _unitOfWork.SaveChangesAsync();

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Retrieve updated book with related data for mapping
                var updatedBook = await _unitOfWork.Repository<Book>().GetAsync(
                    b => b.Id == book.Id,
                    b => b.Categories,
                    b => b.Copies);

                // Map to DTO
                var bookDetailDto = _mapper.Map<BookDetailDto>(updatedBook);
                
                _logger.LogInformation("Book updated successfully: {BookId} - {BookTitle}", book.Id, book.Title);
                
                return Result.Success(bookDetailDto);
            }
            catch
            {
                // Rollback transaction on error
                await _unitOfWork.RollbackTransactionAsync();
                throw; // Rethrow to be caught by outer try-catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book: {Message}", ex.Message);
            return Result.Failure<BookDetailDto>($"Failed to update book: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a book by ID
    /// Implements UC012 (Delete Book)
    /// </summary>
    public async Task<Result<bool>> DeleteBookAsync(int id)
    {
        try
        {
            // Retrieve book with all related entities
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == id,
                b => b.Categories,
                b => b.Copies);

            if (book == null)
            {
                _logger.LogWarning("Book deletion failed: Book not found with ID {BookId}", id);
                return Result.Failure<bool>($"Book with ID {id} not found.");
            }

            // Check for active loans
            var hasActiveLoansResult = await BookHasActiveLoansAsync(id);
            if (hasActiveLoansResult.IsSuccess && hasActiveLoansResult.Value)
            {
                _logger.LogWarning("Book deletion failed: Book {BookId} has active loans", id);
                return Result.Failure<bool>("Cannot delete book because it has active loans.");
            }

            // Check for active reservations
            var hasActiveReservationsResult = await BookHasActiveReservationsAsync(id);
            if (hasActiveReservationsResult.IsSuccess && hasActiveReservationsResult.Value)
            {
                _logger.LogWarning("Book deletion failed: Book {BookId} has active reservations", id);
                return Result.Failure<bool>("Cannot delete book because it has active reservations.");
            }

            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Delete all copies first
                foreach (var copy in book.Copies.ToList())
                {
                    _unitOfWork.Repository<BookCopy>().Delete(copy);
                }

                // Clear category relationships
                book.Categories.Clear();

                // Delete book
                _unitOfWork.Repository<Book>().Delete(book);
                await _unitOfWork.SaveChangesAsync();

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Book deleted successfully: {BookId} - {BookTitle}", id, book.Title);
                return Result.Success(true);
            }
            catch
            {
                // Rollback transaction on error
                await _unitOfWork.RollbackTransactionAsync();
                throw; // Rethrow to be caught by outer try-catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to delete book: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a book by ID with detailed information
    /// Implements UC013 (Search Books) - detailed view
    /// </summary>
    public async Task<Result<BookDetailDto>> GetBookByIdAsync(int id)
    {
        try
        {
            // Retrieve book with all related entities
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == id,
                b => b.Categories,
                b => b.Copies);

            if (book == null)
            {
                _logger.LogWarning("Get book details failed: Book not found with ID {BookId}", id);
                return Result.Failure<BookDetailDto>($"Book with ID {id} not found.");
            }

            // Map to DTO
            var bookDetailDto = _mapper.Map<BookDetailDto>(book);
            
            _logger.LogInformation("Retrieved book details: {BookId} - {BookTitle}", id, book.Title);
            return Result.Success(bookDetailDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book details: {Message}", ex.Message);
            return Result.Failure<BookDetailDto>($"Failed to retrieve book details: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a paginated list of books based on search parameters
    /// Implements UC013 (Search Books) and UC014 (Browse by Category)
    /// </summary>
    public async Task<Result<PagedResult<BookBasicDto>>> SearchBooksAsync(BookSearchRequest request)
    {
        try
        {
            // Set up predicate for filtering
            Expression<Func<Book, bool>>? predicate = null;
            
            // Combine search term and category filter if both are provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm) && request.CategoryId.HasValue)
            {
                var searchTerm = request.SearchTerm.ToLower();
                var categoryId = request.CategoryId.Value;

				predicate = b => (b.Title.ToLower().Contains(searchTerm.ToLower()) ||
								 b.Author.ToLower().Contains(searchTerm.ToLower()) ||
								 b.ISBN.ToLower().Contains(searchTerm.ToLower())) &&
                                 b.Categories.Any(c => c.Id == categoryId);
            }
            // Filter by search term only
            else if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                
                predicate = b => b.Title.ToLower().Contains(searchTerm.ToLower()) || 
                                b.Author.ToLower().Contains(searchTerm.ToLower()) || 
                                b.ISBN.ToLower().Contains(searchTerm.ToLower());
            }
            // Filter by category only
            else if (request.CategoryId.HasValue)
            {
                var categoryId = request.CategoryId.Value;
                
                predicate = b => b.Categories.Any(c => c.Id == categoryId);
            }

            // Apply pagination
            var pagedResult = await _unitOfWork.Repository<Book>().PagedListAsync(
				request,
                predicate,
                q => q.OrderBy(b => b.Title),
                true,
                b => b.Categories,
                b => b.Copies);

            // Map to DTOs
            var bookDtoList = _mapper.Map<IEnumerable<BookBasicDto>>(pagedResult.Items);
            
            var paginatedResult = new PagedResult<BookBasicDto>
            {
                Items = [.. bookDtoList],
                Count = pagedResult.Count,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };
            
            _logger.LogInformation(
                "Retrieved paginated books, Page: {Page}, PageSize: {PageSize}, TotalCount: {TotalCount}, SearchTerm: {SearchTerm}, CategoryId: {CategoryId}", 
                paginatedResult.Page, 
                paginatedResult.PageSize, 
                paginatedResult.Count,
                request.SearchTerm ?? "(none)",
                request.CategoryId?.ToString() ?? "(none)");
            
            return Result.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books: {Message}", ex.Message);
            return Result.Failure<PagedResult<BookBasicDto>>($"Failed to search books: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a book with the specified ISBN already exists
    /// Supports validation for UC010 (Add Book) and UC011 (Update Book)
    /// </summary>
    public async Task<Result<bool>> IsbnExistsAsync(string isbn, int? excludeId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return Result.Failure<bool>("ISBN cannot be empty.");
            }

            Expression<Func<Book, bool>> predicate;
            if (excludeId.HasValue)
            {
				predicate = b => b.ISBN.ToLower() == isbn && b.Id != excludeId.Value;
			}
            else
            {
				predicate = b => b.ISBN.ToLower() == isbn;
			}

            var exists = await _unitOfWork.Repository<Book>().ExistsAsync(predicate);
            return Result.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if ISBN exists: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if ISBN exists: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a book has any active loans
    /// Supports validation for UC012 (Delete Book)
    /// </summary>
    public async Task<Result<bool>> BookHasActiveLoansAsync(int id)
    {
        try
        {
            var hasActiveLoans = await _unitOfWork.Repository<BookCopy>().ExistsAsync(
                c => c.BookId == id && c.Loans.Any(l => l.Status == LoanStatus.Active));
            
            return Result.Success(hasActiveLoans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if book has active loans: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if book has active loans: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a book has any active reservations
    /// Supports validation for UC012 (Delete Book)
    /// </summary>
    public async Task<Result<bool>> BookHasActiveReservationsAsync(int id)
    {
        try
        {
            var hasActiveReservations = await _unitOfWork.Repository<BookCopy>().ExistsAsync(
                c => c.BookId == id && c.Reservations.Any(r => r.Status == ReservationStatus.Active));
            
            return Result.Success(hasActiveReservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if book has active reservations: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if book has active reservations: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to generate a unique copy number for a book
    /// </summary>
    private async Task<Result<string>> GenerateUniqueCopyNumberAsync(int bookId, int copyIndex)
    {
        var book = await _unitOfWork.Repository<Book>().GetAsync(b => b.Id == bookId);
        if (book == null)
        {
            return Result.Failure<string>($"Book with ID {bookId} not found.");
		}

        // ISBN-XXX format (replace hyphens with empty string for cleaner copy numbers)
        var isbnBase = book.ISBN.Replace("-", "");
        var copyNumber = $"{isbnBase}-{copyIndex:D3}"; // Formats as 001, 002, etc.

        // Check if copy number already exists
        var exists = await _unitOfWork.Repository<BookCopy>().ExistsAsync(
            c => c.BookId == bookId && c.CopyNumber == copyNumber);

        // If exists, try next number
        if (exists)
        {
            return await GenerateUniqueCopyNumberAsync(bookId, copyIndex + 1);
        }

        return Result.Success(copyNumber);
	}
}