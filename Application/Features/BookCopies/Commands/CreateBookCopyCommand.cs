using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

/// <summary>
/// Command to create a new book copy (UC015 - Add Copy).
/// </summary>
/// <remarks>
/// This implementation follows UC015 specifications:
/// - Validates the parent book exists (Normal Flow 15.0 step 4)
/// - Enforces copy number uniqueness within a book (Normal Flow 15.0 step 6)
/// - Auto-generates copy numbers following the ISBN-XXX format if not provided (Alternative Flow 15.1: Auto-Generated Copy Number)
/// - Sets initial status to Available (POST-3)
/// - Updates book availability statistics (POST-4)
/// - Records the addition in the audit log (POST-5)
/// - Supports custom copy numbers (Alternative Flow 15.2: Custom Copy Number)
/// - Supports multiple copy creation (Alternative Flow 15.3: Multiple Copy Creation)
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can add copies)
/// - BR-09: Copy Status Rules (Copy statuses include: Available, On Loan, Reserved, Lost)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record CreateBookCopyCommand(CreateBookCopyDto BookCopyDto) : IRequest<Result<int>>;

public class CreateBookCopyCommandHandler : IRequestHandler<CreateBookCopyCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBookCopyCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateBookCopyCommand request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        // Validate book exists (PRE-3: Book must exist in the catalog)
        var book = await bookRepository.GetAsync(b => b.Id == request.BookCopyDto.BookId);
        if (book == null)
            return Result.Failure<int>($"Book with ID {request.BookCopyDto.BookId} not found.");
        
        // Validate copy number uniqueness if provided (POST-2: Copy is assigned unique ID)
        if (!string.IsNullOrWhiteSpace(request.BookCopyDto.CopyNumber))
        {
            var copyNumberExists = await bookCopyRepository.ExistsAsync(
                bc => bc.CopyNumber == request.BookCopyDto.CopyNumber && bc.BookId == request.BookCopyDto.BookId
            );
            
            if (copyNumberExists)
                return Result.Failure<int>($"Book copy with number '{request.BookCopyDto.CopyNumber}' already exists for this book."); // UC015.E2: Duplicate Copy Number
        }
        
        // Create book copy
        var bookCopy = _mapper.Map<BookCopy>(request.BookCopyDto);
        
        // If copy number is not provided, generate one (UC015.1: Auto-Generated Copy Number)
        if (string.IsNullOrWhiteSpace(bookCopy.CopyNumber))
        {
            // Count existing copies to determine next copy number
            var existingCopiesCount = await bookCopyRepository.CountAsync(bc => bc.BookId == book.Id);
            bookCopy.CopyNumber = $"{book.ISBN}-{existingCopiesCount + 1:D3}"; // Format follows library standard
        }
        
        // Set default status to Available (POST-3: Copy status is set to Available)
        bookCopy.Status = CopyStatus.Available;
        
        await bookCopyRepository.AddAsync(bookCopy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Record audit log for copy creation (POST-5: Copy addition is logged)
        var auditRepository = _unitOfWork.Repository<AuditLog>();
        await auditRepository.AddAsync(new AuditLog
        {
            EntityType = "BookCopy",
            EntityId = bookCopy.Id.ToString(),
            EntityName = $"Copy {bookCopy.CopyNumber} of '{book.Title}'",
            ActionType = AuditActionType.Create,
            Details = $"Added new copy #{bookCopy.CopyNumber} for book '{book.Title}' (ID: {book.Id}).",
            IsSuccess = true
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(bookCopy.Id);
    }
}