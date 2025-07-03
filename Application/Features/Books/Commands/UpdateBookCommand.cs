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
/// - Updates basic book information (title, author, publisher, etc.)
/// - Updates book status (Available, Unavailable, UnderMaintenance)
/// - Updates category associations
/// - Maintains modification timestamp
/// - Records audit information for the change
/// </remarks>
public record UpdateBookCommand(int Id, UpdateBookDto BookDto) : IRequest<Result>;

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
        
        return Result.Success();
    }
}