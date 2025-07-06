using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for audit logging operations.
/// Supports Business Rule: BR-22 (Audit Logging Requirement).
/// </summary>
public interface IAuditService
{
    Task<Result> CreateAuditLogAsync(CreateAuditLogRequest request);
    
    Task<Result<PagedResult<AuditLogResponse>>> SearchAuditLogsAsync(AuditLogSearchRequest request);
}
