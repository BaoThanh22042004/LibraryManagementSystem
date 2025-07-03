using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

/// <summary>
/// Command to extend the due date of a loan (UC020 - Renew Loan).
/// </summary>
/// <remarks>
/// This implementation follows UC020 specifications:
/// - Validates loan exists with active status
/// - Validates member has active membership
/// - Ensures new due date is after current due date
/// - Enforces maximum extension period (30 days)
/// - Records extension transaction for audit purposes
/// - Updates loan record with new due date
/// </remarks>
public record ExtendLoanCommand(ExtendLoanDto ExtendLoanDto) : IRequest<Result>;

public class ExtendLoanCommandHandler : IRequestHandler<ExtendLoanCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ExtendLoanCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ExtendLoanCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get loan with member information (PRE-2: Loan must exist with active status)
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.ExtendLoanDto.LoanId,
                l => l.Member,
                l => l.BookCopy,
                l => l.BookCopy.Book
            );
            
            if (loan == null)
                return Result.Failure($"Loan with ID {request.ExtendLoanDto.LoanId} not found."); // UC020.E1: Loan Not Found
            
            // Check if loan is active (PRE-2: Loan must exist with active status)
            if (loan.Status != LoanStatus.Active)
                return Result.Failure("Only active loans can be extended."); // UC020.E2: Loan Not Active
            
            // Store original due date for audit purposes
            var originalDueDate = loan.DueDate;
            
            // Check if new due date is valid (PRE-4: New due date must be valid and within extension limits)
            if (request.ExtendLoanDto.NewDueDate <= loan.DueDate)
                return Result.Failure("New due date must be after the current due date."); // UC020.E3: Invalid Extension Date
            
            // Check if new due date is within maximum extension period (UC020.1: Maximum Extension Period Applied)
            var maxExtensionDate = DateTime.Now.AddDays(30);
            if (request.ExtendLoanDto.NewDueDate > maxExtensionDate)
                return Result.Failure($"New due date cannot be more than 30 days from now ({maxExtensionDate:yyyy-MM-dd})."); // UC020.E4: Extension Limit Exceeded
            
            // Check if member is active (PRE-3: Member must have active membership standing)
            if (loan.Member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure("Loan cannot be extended because member is not active."); // UC020.E5: Member Not Active
            
            // Check if there are active reservations for this book (PRE-5: No active reservations exist for the book)
            bool hasActiveReservations = await reservationRepository.ExistsAsync(r => 
                r.BookId == loan.BookCopy.BookId && 
                r.Status == ReservationStatus.Active);
                
            if (hasActiveReservations)
                return Result.Failure("Cannot extend loan because there are active reservations for this book.");
            
            // Update due date (POST-1: Loan due date is updated to the new extended date)
            loan.DueDate = request.ExtendLoanDto.NewDueDate;
            
            loanRepository.Update(loan);
            
            // Record extension in audit log (POST-2: Extension transaction is recorded for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Loan",
                EntityId = loan.Id.ToString(),
                EntityName = $"Extension of loan for '{loan.BookCopy.Book.Title}'",
                ActionType = AuditActionType.Update,
                Details = $"Due date extended from {originalDueDate:yyyy-MM-dd} to {loan.DueDate:yyyy-MM-dd}" + 
                          (!string.IsNullOrEmpty(request.ExtendLoanDto.Reason) ? $". Reason: {request.ExtendLoanDto.Reason}" : ""),
                BeforeState = originalDueDate.ToString("yyyy-MM-dd"),
                AfterState = loan.DueDate.ToString("yyyy-MM-dd"),
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to extend loan: {ex.Message}");
        }
    }
}