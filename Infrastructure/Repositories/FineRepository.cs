using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FineRepository : Repository<Fine>, IFineRepository
{
    public FineRepository(LibraryDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Fine>> GetUnpaidFinesAsync()
    {
        return await _dbSet
            .Include(f => f.Member)
            .ThenInclude(m => m.User)
            .Where(f => f.Status == Domain.Enums.FineStatus.Pending)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedResult<Fine>> GetUnpaidFinesPagedAsync(PagedRequest request)
    {
        var query = _dbSet
            .Include(f => f.Member)
            .ThenInclude(m => m.User)
            .Where(f => f.Status == Domain.Enums.FineStatus.Pending)
            .OrderByDescending(f => f.FineDate)
            .AsNoTracking();

        int total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<Fine>(items, total, request.Page, request.PageSize);
    }

    public async Task<decimal> GetOutstandingFinesForMemberAsync(int memberId)
    {
        return await _dbSet
            .Where(f => f.MemberId == memberId && f.Status == Domain.Enums.FineStatus.Pending)
            .SumAsync(f => f.Amount);
    }
} 