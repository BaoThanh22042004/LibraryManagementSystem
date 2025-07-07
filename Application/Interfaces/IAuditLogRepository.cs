using Domain.Entities;
using Application.Common;

namespace Application.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<PagedResult<AuditLog>> GetAuditLogsPagedAsync(PagedRequest request, DateTime? from = null, DateTime? to = null, int? userId = null, string? actionType = null, string? entityType = null);
    // Add more custom audit log queries as needed
} 