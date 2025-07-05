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
    private readonly IValidator<CreateBookDto> _createValidator;
    private readonly IValidator<UpdateBookDto> _updateValidator;
    private readonly IValidator<BookSearchParametersDto> _searchValidator;
    private readonly IAuditService _auditService;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateBookDto> createValidator,
        IValidator<UpdateBookDto> updateValidator,
        IValidator<BookSearchParametersDto> searchValidator,
        IAuditService auditService,
        ILogger<BookService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new book with initial copies
    /// Implements UC010 (Add Book)
    /// </summary>
    public async Task<Result<BookDetailDto>> CreateBookAsync(CreateBookDto createBookDto)
    {
        try
        {
            // Validate input
            var validationResult = await _createValidator.ValidateAsync(createBookDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Book creation validation failed: {Errors}", errors);
                return Result.Failure<BookDetailDto>(errors);
            }

            // Check if ISBN already exists
            var isbnExistsResult = await IsbnExistsAsync(createBookDto.ISBN);
            if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
            {
                _logger.LogWarning("Book creation failed: ISBN '{ISBN}' already exists", createBookDto.ISBN);
                return Result.Failure<BookDetailDto>("A book with this ISBN already exists.");
            }

            // Verify that all specified categories exist
            foreach (var categoryId in createBookDto.CategoryIds)
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
                var book = _mapper.Map<Book>(createBookDto);
                
                // Set initial status and timestamps
                book.Status = BookStatus.Available;
                book.CreatedAt = DateTime.UtcNow;

                // Add book to repository
                await _unitOfWork.Repository<Book>().AddAsync(book);
                
                // Add category relationships
                if (createBookDto.CategoryIds.Any())
                {
                    var categories = await _unitOfWork.Repository<Category>().ListAsync(
                        c => createBookDto.CategoryIds.Contains(c.Id));
                    
                    book.Categories = categories.ToList();
                }
                
                // Save to get book ID
                await _unitOfWork.SaveChangesAsync();

                // Create initial copies
                for (int i = 0; i < createBookDto.InitialCopies; i++)
                {
                    var copyNumber = await GenerateUniqueCopyNumberAsync(book.Id, i + 1);
                    
                    var copy = new BookCopy
                    {
                        BookId = book.Id,
                        CopyNumber = copyNumber,
                        Status = CopyStatus.Available,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.Repository<BookCopy>().AddAsync(copy);
                }
                
                // Save changes with copies
                await _unitOfWork.SaveChangesAsync();
                
                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Log the action
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    ActionType = AuditActionType.Create,
                    EntityType = "Book",
                    EntityId = book.Id.ToString(),
                    EntityName = book.Title,
                    Details = $"Created new book: {book.Title} by {book.Author} with {createBookDto.InitialCopies} initial copies",
                    AfterState = System.Text.Json.JsonSerializer.Serialize(new { 
                        Book = book,
                        CategoryIds = createBookDto.CategoryIds,
                        InitialCopies = createBookDto.InitialCopies
                    }),
                    IsSuccess = true
                });

                // Retrieve complete book with related data for mapping
                var createdBook = await _unitOfWork.Repository<Book>().GetAsync(
                    b => b.Id == book.Id,
                    b => b.Categories,
                    b => b.Copies);

                // Map to DTO
                var bookDetailDto = _mapper.Map<BookDetailDto>(createdBook);
                
                _logger.LogInformation("Book created successfully: {BookId} - {BookTitle} with {CopyCount} copies", 
                    book.Id, book.Title, createBookDto.InitialCopies);
                
                return Result.Success(bookDetailDto);
            }
            catch (Exception ex)
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
    public async Task<Result<BookDetailDto>> UpdateBookAsync(UpdateBookDto updateBookDto)
    {
        try
        {
            // Validate input
            var validationResult = await _updateValidator.ValidateAsync(updateBookDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Book update validation failed: {Errors}", errors);
                return Result.Failure<BookDetailDto>(errors);
            }

            // Retrieve existing book with categories
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == updateBookDto.Id,
                b => b.Categories,
                b => b.Copies);

            if (book == null)
            {
                _logger.LogWarning("Book update failed: Book not found with ID {BookId}", updateBookDto.Id);
                return Result.Failure<BookDetailDto>($"Book with ID {updateBookDto.Id} not found.");
            }

            // Check if updated ISBN already exists (excluding current book)
            if (book.ISBN != updateBookDto.ISBN)
            {
                var isbnExistsResult = await IsbnExistsAsync(updateBookDto.ISBN, updateBookDto.Id);
                if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
                {
                    _logger.LogWarning("Book update failed: ISBN '{ISBN}' already exists", updateBookDto.ISBN);
                    return Result.Failure<BookDetailDto>("A book with this ISBN already exists.");
                }
            }

            // Verify that all specified categories exist
            foreach (var categoryId in updateBookDto.CategoryIds)
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
                // Save original state for audit
                var originalState = System.Text.Json.JsonSerializer.Serialize(new {
                    Book = book,
                    CategoryIds = book.Categories.Select(c => c.Id).ToList()
                });

                // Update book properties
                book.Title = updateBookDto.Title;
                book.Author = updateBookDto.Author;
                book.ISBN = updateBookDto.ISBN;
                book.Publisher = updateBookDto.Publisher;
                book.Description = updateBookDto.Description;
                book.CoverImageUrl = updateBookDto.CoverImageUrl;
                book.PublicationDate = updateBookDto.PublicationDate;
                book.Status = updateBookDto.Status;
                book.LastModifiedAt = DateTime.UtcNow;

                // Update category relationships
                // Clear existing categories
                book.Categories.Clear();
                
                // Get and add updated categories
                var categories = await _unitOfWork.Repository<Category>().ListAsync(
                    c => updateBookDto.CategoryIds.Contains(c.Id));
                
                foreach (var category in categories)
                {
                    book.Categories.Add(category);
                }

                // Update in repository
                _unitOfWork.Repository<Book>().Update(book);
                await _unitOfWork.SaveChangesAsync();

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Log the action
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    ActionType = AuditActionType.Update,
                    EntityType = "Book",
                    EntityId = book.Id.ToString(),
                    EntityName = book.Title,
                    Details = $"Updated book: {book.Title} by {book.Author}",
                    BeforeState = originalState,
                    AfterState = System.Text.Json.JsonSerializer.Serialize(new {
                        Book = book,
                        CategoryIds = book.Categories.Select(c => c.Id).ToList()
                    }),
                    IsSuccess = true
                });

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
                // Save original state for audit
                var originalState = System.Text.Json.JsonSerializer.Serialize(new {
                    Book = book,
                    CategoryIds = book.Categories.Select(c => c.Id).ToList(),
                    CopyCount = book.Copies.Count
                });

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

                // Log the action
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    ActionType = AuditActionType.Delete,
                    EntityType = "Book",
                    EntityId = id.ToString(),
                    EntityName = book.Title,
                    Details = $"Deleted book: {book.Title} by {book.Author} with {book.Copies.Count} copies",
                    BeforeState = originalState,
                    IsSuccess = true
                });

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
    public async Task<Result<PaginatedBooksDto>> SearchBooksAsync(BookSearchParametersDto searchParams)
    {
        try
        {
            // Validate search parameters
            var validationResult = await _searchValidator.ValidateAsync(searchParams);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Book search validation failed: {Errors}", errors);
                return Result.Failure<PaginatedBooksDto>(errors);
            }

            // Set up predicate for filtering
            Expression<Func<Book, bool>>? predicate = null;
            
            // Combine search term and category filter if both are provided
            if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm) && searchParams.CategoryId.HasValue)
            {
                var searchTerm = searchParams.SearchTerm.ToLower();
                var categoryId = searchParams.CategoryId.Value;
                
                predicate = b => (b.Title.ToLower().Contains(searchTerm) || 
                                 b.Author.ToLower().Contains(searchTerm) || 
                                 b.ISBN.ToLower().Contains(searchTerm)) &&
                                 b.Categories.Any(c => c.Id == categoryId);
            }
            // Filter by search term only
            else if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            {
                var searchTerm = searchParams.SearchTerm.ToLower();
                
                predicate = b => b.Title.ToLower().Contains(searchTerm) || 
                                b.Author.ToLower().Contains(searchTerm) || 
                                b.ISBN.ToLower().Contains(searchTerm);
            }
            // Filter by category only
            else if (searchParams.CategoryId.HasValue)
            {
                var categoryId = searchParams.CategoryId.Value;
                
                predicate = b => b.Categories.Any(c => c.Id == categoryId);
            }

            // Apply pagination
            var pagedRequest = new PagedRequest(searchParams.PageNumber, searchParams.PageSize);
            var pagedResult = await _unitOfWork.Repository<Book>().PagedListAsync(
                pagedRequest,
                predicate,
                q => q.OrderBy(b => b.Title),
                true,
                b => b.Categories,
                b => b.Copies);

            // Map to DTOs
            var bookDtoList = _mapper.Map<IEnumerable<BookBasicDto>>(pagedResult.Items);
            
            var paginatedResult = new PaginatedBooksDto
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
                searchParams.SearchTerm ?? "(none)",
                searchParams.CategoryId?.ToString() ?? "(none)");
            
            return Result.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books: {Message}", ex.Message);
            return Result.Failure<PaginatedBooksDto>($"Failed to search books: {ex.Message}");
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
                predicate = b => b.ISBN.ToLower() == isbn.ToLower() && b.Id != excludeId.Value;
            }
            else
            {
                predicate = b => b.ISBN.ToLower() == isbn.ToLower();
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
    private async Task<string> GenerateUniqueCopyNumberAsync(int bookId, int copyIndex)
    {
        var book = await _unitOfWork.Repository<Book>().GetAsync(b => b.Id == bookId);
        if (book == null)
        {
            throw new InvalidOperationException($"Book with ID {bookId} not found.");
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

        return copyNumber;
    }
}