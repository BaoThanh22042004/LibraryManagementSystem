using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Queries;

/// <summary>
/// Query to calculate the fine amount for an overdue loan (UC026 - Calculate Fine).
/// </summary>
/// <remarks>
/// This implementation follows UC026 specifications:
/// - Validates loan exists and is in active/overdue status
/// - Checks if the loan due date has passed
/// - Calculates the number of days overdue
/// - Applies the daily fine rate ($0.50 per day)
/// - Returns the calculated fine amount
/// - Returns zero for loans that are not overdue
/// </remarks>
public record CalculateOverdueFineQuery(int LoanId) : IRequest<decimal>;

public class CalculateOverdueFineQueryHandler : IRequestHandler<CalculateOverdueFineQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly decimal _dailyFineRate = 0.50m; // $0.50 per day overdue

    public CalculateOverdueFineQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> Handle(CalculateOverdueFineQuery request, CancellationToken cancellationToken)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        // Get the loan (PRE-1: Valid loan record must exist in the system)
        var loan = await loanRepository.GetAsync(
            l => l.Id == request.LoanId,
            l => l.BookCopy,
            l => l.BookCopy.Book
        );
        
        if (loan == null)
            return 0; // Return 0 if loan doesn't exist (UC026.E1: Loan Not Found)
        
        // Check if loan has valid due date (PRE-2: Loan must have a valid due date)
        if (loan.DueDate == default)
            return 0;
        
        // Check if loan is in a status where fines are applicable
        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
            return 0; // Not an active or overdue loan (UC026.E2: Invalid Loan Status)
        
        // If the loan is not yet overdue
        if (loan.DueDate > DateTime.Now)
            return 0; // Alternative flow 26.2: No Fine Required
        
        // Calculate days overdue (POST-1: Fine amount is calculated based on days overdue)
        int daysOverdue = (int)Math.Ceiling((DateTime.Now - loan.DueDate).TotalDays);
        
        // Apply daily fine rate (PRE-3: Daily fine rate is established)
        decimal fineAmount = daysOverdue * _dailyFineRate;
        
        return fineAmount; // Return calculated amount (Normal flow step 7)
    }
}