using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Queries;

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
        
        // Get the loan
        var loan = await loanRepository.GetAsync(l => l.Id == request.LoanId);
        
        if (loan == null)
            return 0; // Return 0 if loan doesn't exist
        
        // Check if loan is overdue
        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
            return 0; // Not an active or overdue loan
        
        // If the loan is not yet overdue
        if (loan.DueDate > DateTime.Now)
            return 0;
        
        // Calculate days overdue
        int daysOverdue = (int)(DateTime.Now - loan.DueDate).TotalDays;
        
        // Calculate fine amount
        decimal fineAmount = daysOverdue * _dailyFineRate;
        
        return fineAmount;
    }
}