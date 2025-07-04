using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Books.Commands;

/// <summary>
/// Command to create a new book in the catalog (UC010 - Add Book).
/// </summary>
/// <remarks>
/// This implementation follows UC010 specifications:
/// - Creates a new book with provided details (Normal Flow 10.0 steps 11-15)
/// - Validates ISBN uniqueness (UC010.E1: Duplicate ISBN Found)
/// - Validates input data format (UC010.E2: Invalid Input Data)
/// - Handles category loading errors (UC010.E3: Category Loading Error)
/// - Validates cover image format if provided (UC010.E4: Image Upload Failure)
/// - Assigns book to selected categories (Alternative Flow 10.2: Multiple Categories Selection)
/// - Automatically generates initial book copies with sequential copy numbers (Alternative Flow 10.3: Bulk Copy Creation)
/// - Sets book status to Available by default (POST-2)
/// - Records audit information for the change (POST-6)
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can add books)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record CreateBookCommand(CreateBookDto BookDto) : IRequest<Result<int>>;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBookCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookRepository = _unitOfWork.Repository<Book>();
            var categoryRepository = _unitOfWork.Repository<Category>();
            
            // PRE-4: At least one category must exist in the system (if categories are provided)
            if (request.BookDto.CategoryIds.Count > 0)
            {
                var categoriesExist = await categoryRepository.ExistsAsync(c => request.BookDto.CategoryIds.Contains(c.Id));
                if (!categoriesExist)
                {
                    return Result.Failure<int>("One or more selected categories do not exist. Please verify category selection."); // UC010.E3: Category Loading Error
                }
            }
            
            // UC010.E1: Duplicate ISBN Found
            var isbnExists = await bookRepository.ExistsAsync(b => b.ISBN == request.BookDto.ISBN);
            if (isbnExists)
            {
                return Result.Failure<int>($"A book with ISBN {request.BookDto.ISBN} already exists. Please provide a different ISBN or verify the book doesn't already exist.");
            }

            // UC010.E4: Image Upload Failure - Validate cover image URL format if provided
            if (!string.IsNullOrWhiteSpace(request.BookDto.CoverImageUrl))
            {
                if (!IsValidImageUrl(request.BookDto.CoverImageUrl))
                {
                    return Result.Failure<int>("Invalid cover image format. Please provide a valid image URL.");
                }
            }

            // Create new book (POST-1, POST-2)
            var book = _mapper.Map<Book>(request.BookDto);
            book.Status = BookStatus.Available; // Books start as Available by default
            
            await bookRepository.AddAsync(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Add categories (POST-4)
            if (request.BookDto.CategoryIds.Count > 0)
            {
                var categories = await categoryRepository.ListAsync(
                    c => request.BookDto.CategoryIds.Contains(c.Id)
                );
                
                book.Categories = categories.ToList();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            
            // Create initial copies (POST-3) - UC010.3: Bulk Copy Creation
            if (request.BookDto.InitialCopiesCount > 0)
            {
                var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
                var copies = new List<BookCopy>();
                
                for (int i = 1; i <= request.BookDto.InitialCopiesCount; i++)
                {
                    var copy = new BookCopy
                    {
                        BookId = book.Id,
                        CopyNumber = $"{book.ISBN}-{i:D3}", // Format follows library standard (UC010.1: Auto-Generated Copy Number)
                        Status = CopyStatus.Available
                    };
                    copies.Add(copy);
                }
                
                await bookCopyRepository.AddRangeAsync(copies);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            
            // Add audit entry for book creation (POST-6)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Book",
                EntityId = book.Id.ToString(),
                EntityName = book.Title,
                ActionType = AuditActionType.Create,
                Details = $"Book '{book.Title}' with ISBN '{book.ISBN}' was added with {request.BookDto.InitialCopiesCount} initial copies.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(book.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"An error occurred while creating the book: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates if the provided URL is a valid image URL
    /// </summary>
    /// <param name="imageUrl">The image URL to validate</param>
    /// <returns>True if the URL is valid, false otherwise</returns>
    private static bool IsValidImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;
            
        // Check for common image file extensions
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var lowerUrl = imageUrl.ToLower();
        
        return validExtensions.Any(ext => lowerUrl.EndsWith(ext)) || 
               lowerUrl.StartsWith("data:image/") ||
               lowerUrl.Contains("blob:") ||
               lowerUrl.StartsWith("http");
    }
}