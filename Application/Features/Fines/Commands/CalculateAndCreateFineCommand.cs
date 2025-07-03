using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

/// <summary>
/// Command to calculate fines for an overdue loan and create a fine record in the system (UC026 - Calculate Fine).
/// </summary>
/// <remarks>
/// This implementation follows UC026 specifications for automatic fine creation:
/// - Validates loan exists with valid due date
/// - Checks if the loan is overdue
/// - Calculates the number of days overdue
/// - Applies the daily fine rate ($0.50 per day)
/// - Creates a fine record with pending status
/// - Links the fine to the loan and member records
/// - Updates member's outstanding balance
/// - Logs the fine creation for audit purposes
/// </remarks>
public record CalculateAndCreateFineCommand(int LoanId) : IRequest<Result<int>>;

public class CalculateAndCreateFineCommandHandler : IRequestHandler<CalculateAndCreateFineCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly decimal _dailyFineRate = 0.50m; // $0.50 per day overdue

    public CalculateAndCreateFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CalculateAndCreateFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get the loan (PRE-1: Valid loan record must exist in the system)
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.LoanId,
                l => l.Member,
                l => l.Member.User,
                l => l.BookCopy,
                l => l.BookCopy.Book
            );
            
            if (loan == null || loan.Member == null || loan.BookCopy?.Book == null)
                return Result.Failure<int>($"Loan with ID {request.LoanId} not found or has missing data."); // UC026.E1: Loan Not Found
            
            // Check if loan has valid due date (PRE-2: Loan must have a valid due date)
            if (loan.DueDate == default)
                return Result.Failure<int>("Loan does not have a valid due date.");
            
            // Check if loan is eligible for fine calculation
            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return Result.Failure<int>($"Fine can only be calculated for active or overdue loans. Current status: {loan.Status}"); // UC026.E2: Invalid Loan Status
                
            // Check if the loan is not yet overdue
            if (loan.DueDate > DateTime.Now)
                return Result.Failure<int>("Loan is not overdue yet."); // Alternative flow 26.2: No Fine Required
            
            // Check if fine already exists for this loan
            var existingFine = await fineRepository.ExistsAsync(f => 
                f.LoanId == request.LoanId && 
                f.Type == FineType.Overdue &&
                f.Status == FineStatus.Pending);
                
            if (existingFine)
                return Result.Failure<int>("An overdue fine already exists for this loan.");
            
            // Calculate days overdue (POST-1: Fine amount is calculated based on days overdue)
            int daysOverdue = (int)Math.Ceiling((DateTime.Now - loan.DueDate).TotalDays);
            
            // Apply daily fine rate (PRE-3: Daily fine rate is established)
            decimal fineAmount = daysOverdue * _dailyFineRate;
            
            // Store book and member details for logging
            var bookTitle = loan.BookCopy.Book.Title;
            var memberName = loan.Member.User?.FullName ?? $"Member ID: {loan.MemberId}";
            
            // Create the fine (POST-2: Fine record is created with pending status)
            var fine = new Fine
            {
                MemberId = loan.MemberId,
                LoanId = loan.Id,
                Type = FineType.Overdue,
                Amount = fineAmount,
                Description = $"Overdue fine for '{bookTitle}'. {daysOverdue} days late at {_dailyFineRate:C} per day.",
                FineDate = DateTime.Now,
                Status = FineStatus.Pending
            };
            
            await fineRepository.AddAsync(fine);
            
            // Update member's outstanding fines (POST-3: Member's outstanding balance is updated)
            var member = loan.Member;
            member.OutstandingFines += fine.Amount;
            memberRepository.Update(member);
            
            // Update loan status to overdue if it's not already
            if (loan.Status != LoanStatus.Overdue)
            {
                loan.Status = LoanStatus.Overdue;
                loanRepository.Update(loan);
            }
            
            // Record fine creation in audit log
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Fine",
                EntityId = fine.Id.ToString(),
                EntityName = $"Overdue Fine",
                ActionType = AuditActionType.FineCreated,
                Details = $"Created overdue fine of {fineAmount:C} for loan of '{bookTitle}' to member '{memberName}'. {daysOverdue} days overdue.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(fine.Id); // POST-4, POST-5: Fine is linked to loan/member and changes saved
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to calculate and create fine: {ex.Message}"); // UC026.E3: System Error
        }
    }
}