using Domain.Entities;
using Application.Common;

namespace Application.Interfaces;

public interface ILoanRepository : IRepository<Loan>
{
    Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime asOfDate);
    Task<PagedResult<Loan>> GetOverdueLoansPagedAsync(PagedRequest request, DateTime asOfDate);
    // Add more custom loan queries as needed
} 