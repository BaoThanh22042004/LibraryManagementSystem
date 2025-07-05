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
/// Implementation of the IBookCopyService interface.
/// Provides book copy management functionality supporting use cases UC015-UC017.
/// </summary>
public class BookCopyService : IBookCopyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateBookCopyDto> _createValidator;
    private readonly IValidator<CreateMultipleBookCopiesDto> _createMultipleValidator;
    private readonly IValidator<UpdateBookCopyStatusDto> _updateStatusValidator;
    private readonly IAuditService _auditService;
    private readonly ILogger<BookCopyService> _logger;

    public BookCopyService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateBookCopyDto> createValidator,
        IValidator<CreateMultipleBookCopiesDto> createMultipleValidator,
        IValidator<UpdateBookCopyStatusDto> updateStatusValidator,
        IAuditService auditService,
        ILogger<BookCopyService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _createMultipleValidator = createMultipleValidator;
        _updateStatusValidator = updateStatusValidator;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new book copy
    /// Implements UC015 (Add Copy)
    /// </summary>
    public async Task<Result<BookCopyDetailDto>> CreateBookCopyAsync(CreateBookCopyDto createBookCopyDto)
    {
        try
        {
            // Validate input
            var validationResult = await _createValidator.ValidateAsync(createBookCopyDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Book copy creation validation failed: {Errors}", errors);
                return Result.Failure<BookCopyDetailDto>(errors);
            }

            // Check if book exists
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == createBookCopyDto.BookId,
                b => b.Copies);
                
            if (book == null)
            {
                _logger.LogWarning("Book copy creation failed: Book not found with ID {BookId}", createBookCopyDto.BookId);
                return Result.Failure<BookCopyDetailDto>($"Book with ID {createBookCopyDto.BookId} not found.");
            }

            // Generate copy number if not provided
            string copyNumber = createBookCopyDto.CopyNumber ?? string.Empty;
            if (string.IsNullOrWhiteSpace(copyNumber))
            {
                var generateResult = await GenerateUniqueCopyNumberAsync(createBookCopyDto.BookId);
                if (generateResult.IsFailure)
                {
                    return Result.Failure<BookCopyDetailDto>(generateResult.Error);
                }
                
                copyNumber = generateResult.Value;
            }
            else
            {
                // Check if copy number is unique for this book
                var exists = await CopyNumberExistsAsync(createBookCopyDto.BookId, copyNumber);
                if (exists.IsSuccess && exists.Value)
                {
                    _logger.LogWarning("Book copy creation failed: Copy number '{CopyNumber}' already exists for book {BookId}", 
                        copyNumber, createBookCopyDto.BookId);
                    return Result.Failure<BookCopyDetailDto>($"Copy number '{copyNumber}' already exists for this book.");
                }
            }

            // Map DTO to entity
            var bookCopy = _mapper.Map<BookCopy>(createBookCopyDto);
            bookCopy.CopyNumber = copyNumber;
            bookCopy.CreatedAt = DateTime.UtcNow;

            // Add to repository
            await _unitOfWork.Repository<BookCopy>().AddAsync(bookCopy);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Create,
                EntityType = "BookCopy",
                EntityId = bookCopy.Id.ToString(),
                EntityName = $"Copy {copyNumber} of Book {book.Title}",
                Details = $"Created new copy {copyNumber} for book: {book.Title} by {book.Author}",
                AfterState = System.Text.Json.JsonSerializer.Serialize(bookCopy),
                IsSuccess = true
            });

            // Retrieve copy with related data for mapping
            var createdCopy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == bookCopy.Id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(createdCopy);
            
            _logger.LogInformation("Book copy created successfully: {CopyId} - {CopyNumber} for Book {BookId}", 
                bookCopy.Id, copyNumber, createBookCopyDto.BookId);
                
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book copy: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>($"Failed to create book copy: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates multiple book copies at once
    /// Implements UC015 (Add Copy) - bulk creation alternative flow
    /// </summary>
    public async Task<Result<IEnumerable<int>>> CreateMultipleBookCopiesAsync(CreateMultipleBookCopiesDto createMultipleDto)
    {
        try
        {
            // Validate input
            var validationResult = await _createMultipleValidator.ValidateAsync(createMultipleDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Multiple book copies creation validation failed: {Errors}", errors);
                return Result.Failure<IEnumerable<int>>(errors);
            }

            // Check if book exists
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == createMultipleDto.BookId,
                b => b.Copies);
                
            if (book == null)
            {
                _logger.LogWarning("Multiple book copies creation failed: Book not found with ID {BookId}", createMultipleDto.BookId);
                return Result.Failure<IEnumerable<int>>($"Book with ID {createMultipleDto.BookId} not found.");
            }

            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();
            
            var createdCopyIds = new List<int>();
            
            try
            {
                for (int i = 0; i < createMultipleDto.Quantity; i++)
                {
                    // Generate unique copy number
                    var generateResult = await GenerateUniqueCopyNumberAsync(createMultipleDto.BookId);
                    if (generateResult.IsFailure)
                    {
                        // Rollback if we can't generate copy numbers
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result.Failure<IEnumerable<int>>(generateResult.Error);
                    }
                    
                    string copyNumber = generateResult.Value;

                    // Create copy
                    var bookCopy = new BookCopy
                    {
                        BookId = createMultipleDto.BookId,
                        CopyNumber = copyNumber,
                        Status = createMultipleDto.Status,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Add to repository
                    await _unitOfWork.Repository<BookCopy>().AddAsync(bookCopy);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdCopyIds.Add(bookCopy.Id);
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Log the action
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    ActionType = AuditActionType.Create,
                    EntityType = "BookCopy",
                    EntityId = createMultipleDto.BookId.ToString(), // Reference the book ID
                    EntityName = $"Multiple copies of Book {book.Title}",
                    Details = $"Created {createMultipleDto.Quantity} new copies for book: {book.Title} by {book.Author}",
                    AfterState = System.Text.Json.JsonSerializer.Serialize(new {
                        BookId = createMultipleDto.BookId,
                        Quantity = createMultipleDto.Quantity,
                        Status = createMultipleDto.Status,
                        CreatedCopyIds = createdCopyIds
                    }),
                    IsSuccess = true
                });

                _logger.LogInformation("Created {Quantity} book copies successfully for Book {BookId}", 
                    createMultipleDto.Quantity, createMultipleDto.BookId);
                    
                return Result.Success<IEnumerable<int>>(createdCopyIds);
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
            _logger.LogError(ex, "Error creating multiple book copies: {Message}", ex.Message);
            return Result.Failure<IEnumerable<int>>($"Failed to create multiple book copies: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a book copy's status
    /// Implements UC016 (Update Copy Status)
    /// </summary>
    public async Task<Result<BookCopyDetailDto>> UpdateBookCopyStatusAsync(UpdateBookCopyStatusDto updateStatusDto)
    {
        try
        {
            // Validate input
            var validationResult = await _updateStatusValidator.ValidateAsync(updateStatusDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Book copy status update validation failed: {Errors}", errors);
                return Result.Failure<BookCopyDetailDto>(errors);
            }

            // Retrieve copy with related data
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == updateStatusDto.Id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            if (copy == null)
            {
                _logger.LogWarning("Book copy status update failed: Copy not found with ID {CopyId}", updateStatusDto.Id);
                return Result.Failure<BookCopyDetailDto>($"Book copy with ID {updateStatusDto.Id} not found.");
            }

            // Validate status change
            if (updateStatusDto.Status == CopyStatus.Available)
            {
                // Check BR-10: A copy cannot be marked as Available unless it has been properly returned
                var hasActiveLoans = copy.Loans.Any(l => l.Status == LoanStatus.Active);
                if (hasActiveLoans)
                {
                    _logger.LogWarning("Book copy status update failed: Cannot mark copy {CopyId} as Available while it has active loans", updateStatusDto.Id);
                    return Result.Failure<BookCopyDetailDto>("Cannot mark copy as Available while it has active loans. Process the return first.");
                }
            }

            // Save original state for audit
            var originalState = System.Text.Json.JsonSerializer.Serialize(copy);

            // Update status
            copy.Status = updateStatusDto.Status;
            copy.LastModifiedAt = DateTime.UtcNow;

            // Update in repository
            _unitOfWork.Repository<BookCopy>().Update(copy);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Update,
                EntityType = "BookCopy",
                EntityId = copy.Id.ToString(),
                EntityName = $"Copy {copy.CopyNumber} of Book {copy.Book.Title}",
                Details = $"Updated status of copy {copy.CopyNumber} for book: {copy.Book.Title} to {updateStatusDto.Status}" + 
                          (!string.IsNullOrEmpty(updateStatusDto.Notes) ? $" with notes: {updateStatusDto.Notes}" : ""),
                BeforeState = originalState,
                AfterState = System.Text.Json.JsonSerializer.Serialize(copy),
                IsSuccess = true
            });

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(copy);
            
            _logger.LogInformation("Book copy status updated successfully: {CopyId} - {CopyNumber} to {Status}", 
                copy.Id, copy.CopyNumber, updateStatusDto.Status);
                
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book copy status: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>($"Failed to update book copy status: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a book copy by ID
    /// Implements UC017 (Remove Copy)
    /// </summary>
    public async Task<Result<bool>> DeleteBookCopyAsync(int id)
    {
        try
        {
            // Retrieve copy with related data
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            if (copy == null)
            {
                _logger.LogWarning("Book copy deletion failed: Copy not found with ID {CopyId}", id);
                return Result.Failure<bool>($"Book copy with ID {id} not found.");
            }

            // Check for active loans (BR-08)
            var hasActiveLoans = copy.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoans)
            {
                _logger.LogWarning("Book copy deletion failed: Copy {CopyId} has active loans", id);
                return Result.Failure<bool>("Cannot delete book copy because it has active loans.");
            }

            // Check for active reservations (BR-08)
            var hasActiveReservations = copy.Reservations.Any(r => r.Status == ReservationStatus.Active);
            if (hasActiveReservations)
            {
                _logger.LogWarning("Book copy deletion failed: Copy {CopyId} has active reservations", id);
                return Result.Failure<bool>("Cannot delete book copy because it has active reservations.");
            }

            // Save original state for audit
            var originalState = System.Text.Json.JsonSerializer.Serialize(copy);
            var copyNumber = copy.CopyNumber;
            var bookTitle = copy.Book.Title;

            // Delete copy
            _unitOfWork.Repository<BookCopy>().Delete(copy);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Delete,
                EntityType = "BookCopy",
                EntityId = id.ToString(),
                EntityName = $"Copy {copyNumber} of Book {bookTitle}",
                Details = $"Deleted copy {copyNumber} for book: {bookTitle}",
                BeforeState = originalState,
                IsSuccess = true
            });

            _logger.LogInformation("Book copy deleted successfully: {CopyId} - {CopyNumber}", id, copyNumber);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book copy: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to delete book copy: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a book copy by ID with detailed information
    /// </summary>
    public async Task<Result<BookCopyDetailDto>> GetBookCopyByIdAsync(int id)
    {
        try
        {
            // Retrieve copy with related data
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            if (copy == null)
            {
                _logger.LogWarning("Get book copy details failed: Copy not found with ID {CopyId}", id);
                return Result.Failure<BookCopyDetailDto>($"Book copy with ID {id} not found.");
            }

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(copy);
            
            _logger.LogInformation("Retrieved book copy details: {CopyId} - {CopyNumber}", id, copy.CopyNumber);
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book copy details: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>($"Failed to retrieve book copy details: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all copies for a specific book
    /// </summary>
    public async Task<Result<IEnumerable<BookCopyBasicDto>>> GetCopiesByBookIdAsync(int bookId)
    {
        try
        {
            // Check if book exists
            var bookExists = await _unitOfWork.Repository<Book>().ExistsAsync(b => b.Id == bookId);
            if (!bookExists)
            {
                _logger.LogWarning("Get copies by book ID failed: Book not found with ID {BookId}", bookId);
                return Result.Failure<IEnumerable<BookCopyBasicDto>>($"Book with ID {bookId} not found.");
            }

            // Retrieve copies
            var copies = await _unitOfWork.Repository<BookCopy>().ListAsync(
                c => c.BookId == bookId,
                q => q.OrderBy(c => c.CopyNumber));

            // Map to DTOs
            var copyDtos = _mapper.Map<IEnumerable<BookCopyBasicDto>>(copies);
            
            _logger.LogInformation("Retrieved copies for book ID {BookId}, Count: {Count}", bookId, copies.Count);
            return Result.Success(copyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving copies by book ID: {Message}", ex.Message);
            return Result.Failure<IEnumerable<BookCopyBasicDto>>($"Failed to retrieve copies for book: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a unique copy number for a book
    /// Used in UC015 (Add Copy) when auto-generating copy numbers
    /// </summary>
    public async Task<Result<string>> GenerateUniqueCopyNumberAsync(int bookId)
    {
        try
        {
            // Check if book exists
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == bookId, 
                b => b.Copies);
                
            if (book == null)
            {
                _logger.LogWarning("Generate unique copy number failed: Book not found with ID {BookId}", bookId);
                return Result.Failure<string>($"Book with ID {bookId} not found.");
            }

            // Get the next copy index
            int nextCopyIndex = 1;
            if (book.Copies.Any())
            {
                nextCopyIndex = book.Copies.Count + 1;
            }

            // Generate copy number using ISBN as base (ISBN-XXX format)
            var isbnBase = book.ISBN.Replace("-", ""); // Remove hyphens for cleaner number
            var copyNumber = $"{isbnBase}-{nextCopyIndex:D3}"; // Format as 001, 002, etc.

            // Check if copy number is unique
            bool isUnique = false;
            int attempt = 0;
            const int maxAttempts = 100; // Prevent infinite loop

            while (!isUnique && attempt < maxAttempts)
            {
                var exists = await CopyNumberExistsAsync(bookId, copyNumber);
                if (exists.IsSuccess && !exists.Value)
                {
                    isUnique = true;
                }
                else
                {
                    // Try next number
                    nextCopyIndex++;
                    copyNumber = $"{isbnBase}-{nextCopyIndex:D3}";
                    attempt++;
                }
            }

            if (!isUnique)
            {
                _logger.LogWarning("Failed to generate unique copy number for book {BookId} after {Attempts} attempts", bookId, maxAttempts);
                return Result.Failure<string>($"Failed to generate a unique copy number after {maxAttempts} attempts.");
            }

            _logger.LogInformation("Generated unique copy number {CopyNumber} for book {BookId}", copyNumber, bookId);
            return Result.Success(copyNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unique copy number: {Message}", ex.Message);
            return Result.Failure<string>($"Failed to generate unique copy number: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a copy number already exists for a specific book
    /// Supports validation for UC015 (Add Copy)
    /// </summary>
    public async Task<Result<bool>> CopyNumberExistsAsync(int bookId, string copyNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(copyNumber))
            {
                return Result.Failure<bool>("Copy number cannot be empty.");
            }

            var exists = await _unitOfWork.Repository<BookCopy>().ExistsAsync(
                c => c.BookId == bookId && c.CopyNumber == copyNumber);
                
            return Result.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if copy number exists: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if copy number exists: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a book copy has any active loans
    /// Supports validation for UC016 (Update Copy Status) and UC017 (Remove Copy)
    /// </summary>
    public async Task<Result<bool>> CopyHasActiveLoansAsync(int id)
    {
        try
        {
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == id,
                c => c.Loans);

            if (copy == null)
            {
                return Result.Failure<bool>($"Book copy with ID {id} not found.");
            }

            var hasActiveLoans = copy.Loans.Any(l => l.Status == LoanStatus.Active);
            return Result.Success(hasActiveLoans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if copy has active loans: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if copy has active loans: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a book copy has any active reservations
    /// Supports validation for UC016 (Update Copy Status) and UC017 (Remove Copy)
    /// </summary>
    public async Task<Result<bool>> CopyHasActiveReservationsAsync(int id)
    {
        try
        {
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == id,
                c => c.Reservations);

            if (copy == null)
            {
                return Result.Failure<bool>($"Book copy with ID {id} not found.");
            }

            var hasActiveReservations = copy.Reservations.Any(r => r.Status == ReservationStatus.Active);
            return Result.Success(hasActiveReservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if copy has active reservations: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if copy has active reservations: {ex.Message}");
        }
    }
}