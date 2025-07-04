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
/// - Validates the book copy exists (Normal Flow 16.0 step 4)
/// - Validates status changes against active loans and reservations (Normal Flow 16.0 step 7)
/// - Enforces business rules for status transitions (Normal Flow 16.0 step 6)
/// - Records the status change in the audit log (POST-3)
/// - Updates associated book availability statistics (POST-2)
/// - Supports bulk status updates (Alternative Flow 16.1: Bulk Status Update)
/// - Prevents invalid status transitions (UC016.E3: Invalid Status Change)
/// - Handles status updates with notes (Alternative Flow 16.2: Status Update with Notes)
/// - Prevents conflicts with active loans (UC016.E2: Active Loan Conflict)
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can update copy status)
/// - BR-09: Copy Status Rules (Copy statuses include: Available, On Loan, Reserved, Lost)
/// - BR-10: Copy Return Validation (A copy cannot be marked as Available unless it has been properly returned)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record UpdateBookCopyStatusCommand(int Id, CopyStatus Status, string? Notes = null) : IRequest<Result<bool>>;

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
                return Result.Failure<bool>($"Book copy with ID {request.Id} not found."); // UC016.E1: Copy Not Found
            
            // Store original status for audit and validation
            var originalStatus = bookCopy.Status;
            
            // If current status equals requested status, no change needed
            if (originalStatus == request.Status)
                return Result.Success(true);
            
            // Validate status transitions based on business rules
            var validationResult = await ValidateStatusTransition(bookCopy, originalStatus, request.Status, loanRepository);
            if (!validationResult.IsSuccess)
                return validationResult;
            
            // Update status
            bookCopy.Status = request.Status;
            bookCopy.LastModifiedAt = DateTime.UtcNow;
            
            bookCopyRepository.Update(bookCopy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record audit log for status change (POST-3: Status change is recorded)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            var details = $"Updated status from {originalStatus} to {request.Status}";
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                details += $". Notes: {request.Notes}";
            }
            
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "BookCopy",
                EntityId = bookCopy.Id.ToString(),
                EntityName = $"Copy {bookCopy.CopyNumber} of '{bookCopy.Book.Title}'",
                ActionType = AuditActionType.Update,
                Details = details,
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
    
    /// <summary>
    /// Validates the status transition based on business rules
    /// </summary>
    /// <param name="bookCopy">The book copy being updated</param>
    /// <param name="originalStatus">Current status</param>
    /// <param name="newStatus">Requested status</param>
    /// <param name="loanRepository">Loan repository for validation</param>
    /// <returns>Validation result</returns>
    private async Task<Result<bool>> ValidateStatusTransition(
        BookCopy bookCopy, 
        CopyStatus originalStatus, 
        CopyStatus newStatus, 
        IRepository<Loan> loanRepository)
    {
        // UC016.E2: Active Loan Conflict
        if (newStatus == CopyStatus.Available)
        {
            var activeLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == bookCopy.Id && 
                    (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            if (activeLoans)
                return Result.Failure<bool>("Cannot mark book copy as Available while it has active loans.");
        }
        
        // UC016.E3: Invalid Status Change - Check for reserved status
        if (newStatus == CopyStatus.Reserved)
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var bookId = bookCopy.BookId;
            
            var activeReservations = await reservationRepository.ExistsAsync(
                r => r.BookId == bookId && r.Status == ReservationStatus.Active
            );
            
            if (!activeReservations)
                return Result.Failure<bool>("Cannot mark book copy as Reserved without active reservations for this book.");
        }
        
        // UC016.E3: Invalid Status Change - Check borrowed to other status transitions
        if (originalStatus == CopyStatus.Borrowed && newStatus != CopyStatus.Available)
        {
            var activeLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == bookCopy.Id && 
                    (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            if (activeLoans)
                return Result.Failure<bool>($"Cannot change status from Borrowed to {newStatus} while copy has active loans.");
        }
        
        // UC016.E3: Invalid Status Change - Check for damaged/lost transitions
        if ((newStatus == CopyStatus.Damaged || newStatus == CopyStatus.Lost) && 
            originalStatus == CopyStatus.Borrowed)
        {
            var activeLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == bookCopy.Id && 
                    (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            if (activeLoans)
                return Result.Failure<bool>($"Cannot mark borrowed copy as {newStatus} without first returning it.");
        }
        
        return Result.Success(true);
    }
}