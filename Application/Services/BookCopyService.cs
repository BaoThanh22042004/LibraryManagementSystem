using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;

namespace Application.Services;

/// <summary>
/// Implementation of the IBookCopyService interface.
/// Provides book copy management functionality supporting use cases UC015-UC017.
/// </summary>
public class BookCopyService : IBookCopyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BookCopyService> _logger;

    public BookCopyService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<BookCopyService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new book copy
    /// Implements UC015 (Add Copy)
    /// </summary>
    public async Task<Result<BookCopyDetailDto>> CreateBookCopyAsync(CreateBookCopyRequest request)
    {
        try
        {
            // Check if book exists
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == request.BookId,
                b => b.Copies);
                
            if (book == null)
            {
                _logger.LogWarning("Book copy creation failed: Book not found with ID {BookId}", request.BookId);
                return Result.Failure<BookCopyDetailDto>("Book not found. Please check the book and try again.");
            }

            // Generate copy number if not provided
            string copyNumber = request.CopyNumber ?? string.Empty;
            if (string.IsNullOrWhiteSpace(copyNumber))
            {
                var generateResult = await GenerateUniqueCopyNumberAsync(request.BookId);
                if (generateResult.IsFailure)
                {
                    return Result.Failure<BookCopyDetailDto>(generateResult.Error);
                }
                
                copyNumber = generateResult.Value;
            }
            else
            {
                // Check if copy number is unique for this book
                var exists = await CopyNumberExistsAsync(request.BookId, copyNumber);
                if (exists.IsSuccess && exists.Value)
                {
                    _logger.LogWarning("Book copy creation failed: Copy number '{CopyNumber}' already exists for book {BookId}", 
                        copyNumber, request.BookId);
                    return Result.Failure<BookCopyDetailDto>("A copy with this number already exists for this book. Please use a different number or let the system auto-generate one.");
                }
            }

            // Map DTO to entity
            var bookCopy = _mapper.Map<BookCopy>(request);
            bookCopy.CopyNumber = copyNumber;
            bookCopy.CreatedAt = DateTime.UtcNow;

            // Add to repository
            await _unitOfWork.Repository<BookCopy>().AddAsync(bookCopy);
            await _unitOfWork.SaveChangesAsync();

            // Retrieve copy with related data for mapping
            var createdCopy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == bookCopy.Id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(createdCopy);
            
            _logger.LogInformation("Book copy created successfully: {CopyId} - {CopyNumber} for Book {BookId}", 
                bookCopy.Id, copyNumber, request.BookId);
                
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book copy: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>("System error: Unable to create book copy. Please try again or contact support if the problem persists.");
        }
    }

    /// <summary>
    /// Creates multiple book copies at once
    /// Implements UC015 (Add Copy) - bulk creation alternative flow
    /// </summary>
    public async Task<Result<IEnumerable<int>>> CreateMultipleBookCopiesAsync(CreateMultipleBookCopiesRequest request)
    {
        try
        {
            // Check if book exists
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == request.BookId,
                b => b.Copies);
                
            if (book == null)
            {
                _logger.LogWarning("Multiple book copies creation failed: Book not found with ID {BookId}", request.BookId);
                return Result.Failure<IEnumerable<int>>("Book not found. Please check the book and try again.");
            }

            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();
            
            var createdCopyIds = new List<int>();
            
            try
            {
                for (int i = 0; i < request.Quantity; i++)
                {
                    // Generate unique copy number
                    var generateResult = await GenerateUniqueCopyNumberAsync(request.BookId);
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
                        BookId = request.BookId,
                        CopyNumber = copyNumber,
                        Status = request.Status,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Add to repository
                    await _unitOfWork.Repository<BookCopy>().AddAsync(bookCopy);
                    await _unitOfWork.SaveChangesAsync();
                    
                    createdCopyIds.Add(bookCopy.Id);
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Created {Quantity} book copies successfully for Book {BookId}", 
                    request.Quantity, request.BookId);
                    
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
            return Result.Failure<IEnumerable<int>>("System error: Unable to create multiple book copies. Please try again or contact support if the problem persists.");
        }
    }

    /// <summary>
    /// Updates a book copy's status
    /// Implements UC016 (Update Copy Status)
    /// </summary>
    public async Task<Result<BookCopyDetailDto>> UpdateBookCopyStatusAsync(UpdateBookCopyStatusRequest request)
    {
        try
        {
            // Retrieve copy with related data
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.Id == request.Id,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            if (copy == null)
            {
                _logger.LogWarning("Book copy status update failed: Copy not found with ID {CopyId}", request.Id);
                return Result.Failure<BookCopyDetailDto>("Book copy not found. Please check the copy and try again.");
            }

            // Validate status change
            if (request.Status == CopyStatus.Available)
            {
                // Check BR-10: A copy cannot be marked as Available unless it has been properly returned
                var hasActiveLoans = copy.Loans.Any(l => l.Status == LoanStatus.Active);
                if (hasActiveLoans)
                {
                    _logger.LogWarning("Book copy status update failed: Cannot mark copy {CopyId} as Available while it has active loans", request.Id);
                    return Result.Failure<BookCopyDetailDto>("Cannot mark copy as Available while it has active loans. Please process the return first.");
                }
            }

            // Update status
            copy.Status = request.Status;
            copy.LastModifiedAt = DateTime.UtcNow;

            // Update in repository
            _unitOfWork.Repository<BookCopy>().Update(copy);
            await _unitOfWork.SaveChangesAsync();

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(copy);
            
            _logger.LogInformation("Book copy status updated successfully: {CopyId} - {CopyNumber} to {Status}", 
                copy.Id, copy.CopyNumber, request.Status);
                
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book copy status: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>("System error: Unable to update book copy status. Please try again or contact support if the problem persists.");
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
                return Result.Failure<bool>("Book copy not found. Please check the copy and try again.");
            }

            // Check for active loans (BR-08)
            var hasActiveLoans = copy.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoans)
            {
                _logger.LogWarning("Book copy deletion failed: Copy {CopyId} has active loans", id);
                return Result.Failure<bool>("Cannot delete this copy because it has active loans. Please process all returns before deleting.");
            }

            // Check for active reservations (BR-08)
            var hasActiveReservations = copy.Reservations.Any(r => r.Status == ReservationStatus.Active);
            if (hasActiveReservations)
            {
                _logger.LogWarning("Book copy deletion failed: Copy {CopyId} has active reservations", id);
                return Result.Failure<bool>("Cannot delete this copy because it has active reservations. Please cancel or reassign reservations before deleting.");
            }

            // Delete copy
            _unitOfWork.Repository<BookCopy>().Delete(copy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Book copy deleted successfully: {CopyId} - {CopyNumber}", id, copy.CopyNumber);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book copy: {Message}", ex.Message);
            return Result.Failure<bool>("System error: Unable to delete book copy. Please try again or contact support if the problem persists.");
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
    /// Gets a book copy by book CopyNumber
    /// </summary>

    public async Task<Result<BookCopyDetailDto>> GetBookCopyByCopyNumberAsync(string bookCopyNumber) 
    {
        try
        {
            // Retrieve copy with related data
            var copy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                c => c.CopyNumber == bookCopyNumber,
                c => c.Book,
                c => c.Loans,
                c => c.Reservations);

            if (copy == null)
            {
                _logger.LogWarning("Get book copy details failed: Copy not found with Copy Number {bookCopyNumber}", bookCopyNumber);
                return Result.Failure<BookCopyDetailDto>($"Book copy with ID {bookCopyNumber} not found.");
            }

            // Map to DTO
            var bookCopyDto = _mapper.Map<BookCopyDetailDto>(copy);

            _logger.LogInformation("Retrieved book copy details: {CopyId} - {CopyNumber}", copy.Id, copy.CopyNumber);
            return Result.Success(bookCopyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book copy details: {Message}", ex.Message);
            return Result.Failure<BookCopyDetailDto>($"Failed to retrieve book copy details: {ex.Message}");
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
            if (book.Copies.Count != 0)
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