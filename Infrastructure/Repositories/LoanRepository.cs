using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LoanRepository : Repository<Loan>, ILoanRepository
{
    public LoanRepository(LibraryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime asOfDate)
    {
        return await _dbSet
            .Include(l => l.Member)
            .ThenInclude(m => m.User)
            .Include(l => l.BookCopy)
            .Where(l => l.DueDate < asOfDate && l.Status == Domain.Enums.LoanStatus.Active)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedResult<Loan>> GetOverdueLoansPagedAsync(PagedRequest request, DateTime asOfDate)
    {
        var query = _dbSet
            .Include(l => l.Member)
            .ThenInclude(m => m.User)
            .Include(l => l.BookCopy)
            .Where(l => l.DueDate < asOfDate && l.Status == Domain.Enums.LoanStatus.Active)
            .OrderBy(l => l.DueDate)
            .AsNoTracking();

        int total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<Loan>(items, total, request.Page, request.PageSize);
    }
} 