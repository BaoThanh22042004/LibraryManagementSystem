using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(LibraryDbContext context) : base(context) { }

    public async Task<PagedResult<AuditLog>> GetAuditLogsPagedAsync(
        PagedRequest request,
        DateTime? from = null,
        DateTime? to = null,
        int? userId = null,
        string? actionType = null,
        string? entityType = null)
    {
        var query = _dbSet
            .Include(a => a.User)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(actionType))
            query = query.Where(a => a.ActionType.ToString() == actionType);
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        query = query.OrderByDescending(a => a.CreatedAt);

        int total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLog>(items, total, request.Page, request.PageSize);
    }
} 