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
/// - Creates a new book with provided details
/// - Validates ISBN uniqueness
/// - Assigns book to selected categories
/// - Automatically generates initial book copies with sequential copy numbers
/// - Sets book status to Available by default
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
        var bookRepository = _unitOfWork.Repository<Book>();
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // PRE-4: At least one category must exist in the system (if categories are provided)
        if (request.BookDto.CategoryIds.Count > 0)
        {
            var categoriesExist = await categoryRepository.ExistsAsync(c => request.BookDto.CategoryIds.Contains(c.Id));
            if (!categoriesExist)
            {
                return Result.Failure<int>("One or more selected categories do not exist.");
            }
        }
        
        // Check if ISBN already exists (UC010.E1: Duplicate ISBN Found)
        var isbnExists = await bookRepository.ExistsAsync(b => b.ISBN == request.BookDto.ISBN);
        if (isbnExists)
        {
            return Result.Failure<int>($"A book with ISBN {request.BookDto.ISBN} already exists.");
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
        
        // Create initial copies (POST-3)
        if (request.BookDto.InitialCopiesCount > 0)
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var copies = new List<BookCopy>();
            
            for (int i = 1; i <= request.BookDto.InitialCopiesCount; i++)
            {
                var copy = new BookCopy
                {
                    BookId = book.Id,
                    CopyNumber = $"{book.ISBN}-{i:D3}", // Format follows library standard
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
        
        return Result.Success(book.Id);
    }
}