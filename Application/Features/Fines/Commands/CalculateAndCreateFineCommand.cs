using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

/// <summary>
/// Command to calculate fines for an overdue loan and create a fine record in the system.
/// </summary>
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
            
            // Get the loan
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.LoanId,
                l => l.Member,
                l => l.BookCopy.Book
            );
            
            if (loan == null)
                return Result.Failure<int>($"Loan with ID {request.LoanId} not found.");
            
            // Check if loan is eligible for fine calculation
            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return Result.Failure<int>("Fine can only be calculated for active or overdue loans.");
                
            // Check if the loan is not yet overdue
            if (loan.DueDate > DateTime.Now)
                return Result.Failure<int>("Loan is not overdue yet.");
            
            // Check if fine already exists for this loan
            var existingFine = await fineRepository.ExistsAsync(f => 
                f.LoanId == request.LoanId && 
                f.Type == FineType.Overdue &&
                f.Status == FineStatus.Pending);
                
            if (existingFine)
                return Result.Failure<int>("An overdue fine already exists for this loan.");
            
            // Calculate days overdue
            int daysOverdue = (int)Math.Ceiling((DateTime.Now - loan.DueDate).TotalDays);
            
            // Calculate fine amount
            decimal fineAmount = daysOverdue * _dailyFineRate;
            
            // Create the fine
            var fine = new Fine
            {
                MemberId = loan.MemberId,
                LoanId = loan.Id,
                Type = FineType.Overdue,
                Amount = fineAmount,
                Description = $"Overdue fine for '{loan.BookCopy.Book.Title}'. {daysOverdue} days late at {_dailyFineRate:C} per day.",
                FineDate = DateTime.Now,
                Status = FineStatus.Pending
            };
            
            await fineRepository.AddAsync(fine);
            
            // Update member's outstanding fines
            var member = loan.Member;
            member.OutstandingFines += fine.Amount;
            memberRepository.Update(member);
            
            // Update loan status to overdue if it's not already
            if (loan.Status != LoanStatus.Overdue)
            {
                loan.Status = LoanStatus.Overdue;
                loanRepository.Update(loan);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(fine.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to calculate and create fine: {ex.Message}");
        }
    }
}