using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Books.Commands;

/// <summary>
/// Command to update an existing book in the catalog (UC011 - Update Book).
/// </summary>
/// <remarks>
/// This implementation follows UC011 specifications:
/// - Updates basic book information (title, author, publisher, etc.) (Normal Flow 11.0 steps 13-15)
/// - Updates book status (Available, Unavailable, UnderMaintenance) (Alternative Flow 11.1: Status Change Only)
/// - Updates category associations (Alternative Flow 11.2: Category Reassignment)
/// - Handles cover image replacement and removal (Alternative Flow 11.3: Cover Image Replacement, 11.4: Cover Image Removal)
/// - Maintains modification timestamp (POST-5)
/// - Detects concurrent edit conflicts (UC011.E5: Concurrent Edit Conflict)
/// - Records audit information for the change (POST-6)
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can edit books)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record UpdateBookCommand(int Id, UpdateBookDto BookDto, DateTime? LastModifiedAt = null) : IRequest<Result>;

public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBookCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookRepository = _unitOfWork.Repository<Book>();
            var categoryRepository = _unitOfWork.Repository<Category>();
            
            // Get existing book (PRE-3: Book must exist in the catalog)
            var book = await bookRepository.GetAsync(
                b => b.Id == request.Id,
                b => b.Categories
            );
            
            if (book == null)
            {
                return Result.Failure($"Book with ID {request.Id} not found."); // UC011.E1: Book Not Found
            }
            
            // UC011.E5: Concurrent Edit Conflict
            if (request.LastModifiedAt.HasValue && book.LastModifiedAt.HasValue)
            {
                if (book.LastModifiedAt.Value > request.LastModifiedAt.Value)
                {
                    return Result.Failure("The book has been modified by another user. Please refresh and review the changes before proceeding.");
                }
            }
            
            // Store original state for audit purposes
            var originalState = new
            {
                book.Title,
                book.Author,
                book.Publisher,
                book.Description,
                book.CoverImageUrl,
                book.PublicationDate,
                book.Status,
                Categories = book.Categories.Select(c => c.Id).ToList()
            };
            
            // Validate status transition
            if (!IsValidStatusTransition(book.Status, request.BookDto.Status))
            {
                return Result.Failure($"Invalid status transition from {book.Status} to {request.BookDto.Status}.");
            }
            
            // Update book properties (POST-1, POST-2)
            book.Title = request.BookDto.Title;
            book.Author = request.BookDto.Author;
            book.Publisher = request.BookDto.Publisher;
            book.Description = request.BookDto.Description;
            book.CoverImageUrl = request.BookDto.CoverImageUrl;
            book.PublicationDate = request.BookDto.PublicationDate;
            book.Status = request.BookDto.Status;
            book.LastModifiedAt = DateTime.UtcNow; // POST-5: Update modification timestamp
            
            // Update categories if provided (POST-3)
            if (request.BookDto.CategoryIds.Count > 0)
            {
                // Validate categories exist
                var categoryCount = await categoryRepository.CountAsync(c => request.BookDto.CategoryIds.Contains(c.Id));
                if (categoryCount != request.BookDto.CategoryIds.Count)
                {
                    return Result.Failure("One or more selected categories do not exist."); // UC011.E2: Invalid Data Update
                }
                
                var categories = await categoryRepository.ListAsync(
                    c => request.BookDto.CategoryIds.Contains(c.Id)
                );
                
                book.Categories.Clear();
                foreach (var category in categories)
                {
                    book.Categories.Add(category);
                }
            }
            
            // Save changes
            bookRepository.Update(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record audit log for update (POST-6)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Book",
                EntityId = book.Id.ToString(),
                EntityName = book.Title,
                ActionType = AuditActionType.Update,
                Details = $"Book '{book.Title}' was updated.",
                BeforeState = System.Text.Json.JsonSerializer.Serialize(originalState),
                AfterState = System.Text.Json.JsonSerializer.Serialize(new
                {
                    book.Title,
                    book.Author,
                    book.Publisher,
                    book.Description,
                    book.CoverImageUrl,
                    book.PublicationDate,
                    book.Status,
                    Categories = book.Categories.Select(c => c.Id).ToList()
                }),
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _unitOfWork.CommitTransactionAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"An error occurred while updating the book: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates if the status transition is allowed
    /// </summary>
    /// <param name="currentStatus">Current book status</param>
    /// <param name="newStatus">New book status</param>
    /// <returns>True if transition is valid, false otherwise</returns>
    private static bool IsValidStatusTransition(BookStatus currentStatus, BookStatus newStatus)
    {
        // All transitions are allowed for book status
        // This can be enhanced with specific business rules if needed
        return true;
    }
}