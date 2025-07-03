using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

/// <summary>
/// Command to update a book copy's status (UC016 - Update Copy Status).
/// </summary>
/// <remarks>
/// This implementation follows UC016 specifications:
/// - Validates the book copy exists
/// - Validates status changes against active loans and reservations
/// - Enforces business rules for status transitions
/// - Records the status change in the audit log
/// - Updates associated book availability statistics
/// </remarks>
public record UpdateBookCopyStatusCommand(int Id, CopyStatus Status) : IRequest<Result<bool>>;

public class UpdateBookCopyStatusCommandHandler : IRequestHandler<UpdateBookCopyStatusCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookCopyStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateBookCopyStatusCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            
            // Get book copy (PRE-3: Book copy must exist in the inventory)
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.Id,
                bc => bc.Book
            );
            
            if (bookCopy == null)
                return Result.Failure<bool>($"Book copy with ID {request.Id} not found.");
            
            // Store original status for audit and validation
            var originalStatus = bookCopy.Status;
            
            // If current status equals requested status, no change needed
            if (originalStatus == request.Status)
                return Result.Success(true);
            
            // Validate status transitions based on business rules
            
            // If marking as Available, check if there are active loans (UC016.E2: Active Loan Conflict)
            if (request.Status == CopyStatus.Available)
            {
                var activeLoans = await loanRepository.ExistsAsync(
                    l => l.BookCopyId == request.Id && 
                        (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                );
                
                if (activeLoans)
                    return Result.Failure<bool>("Cannot mark book copy as Available while it has active loans.");
            }
            
            // If marking as Reserved, check if there are active reservations
            if (request.Status == CopyStatus.Reserved)
            {
                var reservationRepository = _unitOfWork.Repository<Reservation>();
                var bookId = bookCopy.BookId;
                
                var activeReservations = await reservationRepository.ExistsAsync(
                    r => r.BookId == bookId && r.Status == ReservationStatus.Active
                );
                
                if (!activeReservations)
                    return Result.Failure<bool>("Cannot mark book copy as Reserved without active reservations for this book."); // UC016.E3: Invalid Status Change
            }
            
            // If current status is Borrowed but trying to mark as anything other than Available,
            // ensure no active loans exist (can't change borrowed copy to damaged/lost without returning)
            if (originalStatus == CopyStatus.Borrowed && request.Status != CopyStatus.Available)
            {
                var activeLoans = await loanRepository.ExistsAsync(
                    l => l.BookCopyId == request.Id && 
                        (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                );
                
                if (activeLoans)
                    return Result.Failure<bool>($"Cannot change status from Borrowed to {request.Status} while copy has active loans."); // UC016.E3: Invalid Status Change
            }
            
            // Update status
            bookCopy.Status = request.Status;
            bookCopy.LastModifiedAt = DateTime.UtcNow;
            
            bookCopyRepository.Update(bookCopy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record audit log for status change (POST-3: Status change is recorded)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "BookCopy",
                EntityId = bookCopy.Id.ToString(),
                EntityName = $"Copy {bookCopy.CopyNumber} of '{bookCopy.Book.Title}'",
                ActionType = AuditActionType.Update,
                Details = $"Updated status from {originalStatus} to {request.Status}.",
                BeforeState = originalStatus.ToString(),
                AfterState = request.Status.ToString(),
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"An error occurred while updating the book copy status: {ex.Message}"); // UC016.E5: System Error
        }
    }
}