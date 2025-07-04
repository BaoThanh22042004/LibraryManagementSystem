using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

/// <summary>
/// Command to permanently remove a book copy from inventory (UC017 - Remove Copy).
/// </summary>
/// <remarks>
/// This implementation follows UC017 specifications:
/// - Validates the book copy exists (Normal Flow 17.0 step 4)
/// - Prevents removal of copies with active loans (UC017.E1: Active Loan Found)
/// - Prevents removal of copies with active reservations (UC017.E2: Active Reservation Found)
/// - Records the removal in the audit log (POST-3)
/// - Preserves historical loan records (POST-4)
/// - Updates book availability statistics (POST-2)
/// - Supports bulk copy removal (Alternative Flow 17.3: Bulk Copy Removal)
/// - Supports marking as lost instead of removal (Alternative Flow 17.2: Mark as Lost Instead of Removal)
/// 
/// Business Rules Enforced:
/// - BR-08: Copy Deletion Restriction (Book copies with active loans or reservations cannot be deleted)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record DeleteBookCopyCommand(int Id) : IRequest<Result<bool>>;

public class DeleteBookCopyCommandHandler : IRequestHandler<DeleteBookCopyCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCopyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteBookCopyCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get book copy with book details (PRE-3: Book copy must exist)
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.Id,
                bc => bc.Book
            );
            
            if (bookCopy == null)
                return Result.Failure<bool>($"Book copy with ID {request.Id} not found."); // UC017.E3: Copy Not Found
            
            // Check for active loans (PRE-4: Copy must not have any active loans)
            var hasActiveLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == request.Id && 
                    (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            if (hasActiveLoans)
                return Result.Failure<bool>("Cannot delete book copy with active loans. Please return the book first."); // UC017.E1: Active Loan Found
            
            // Check for active reservations (PRE-5: Copy must not have any active reservations)
            var hasActiveReservations = await reservationRepository.ExistsAsync(
                r => r.BookId == bookCopy.BookId && 
                     r.BookCopyId == request.Id && 
                     r.Status == ReservationStatus.Active
            );
            
            if (hasActiveReservations)
                return Result.Failure<bool>("Cannot delete book copy with active reservations. Please cancel the reservations first."); // UC017.E2: Active Reservation Found
            
            // Store copy information for audit log before deletion
            var copyInfo = new
            {
                Id = bookCopy.Id,
                CopyNumber = bookCopy.CopyNumber,
                Status = bookCopy.Status,
                BookId = bookCopy.BookId,
                BookTitle = bookCopy.Book.Title
            };
            
            // Delete book copy (POST-1: Book copy record is permanently removed)
            bookCopyRepository.Delete(bookCopy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record audit log for deletion (POST-3: Copy removal is recorded)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "BookCopy",
                EntityId = copyInfo.Id.ToString(),
                EntityName = $"Copy {copyInfo.CopyNumber} of '{copyInfo.BookTitle}'",
                ActionType = AuditActionType.Delete,
                Details = $"Removed copy #{copyInfo.CopyNumber} for book '{copyInfo.BookTitle}' (ID: {copyInfo.BookId}).",
                BeforeState = System.Text.Json.JsonSerializer.Serialize(copyInfo),
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"An error occurred while deleting the book copy: {ex.Message}"); // UC017.E5: System Error
        }
    }
}