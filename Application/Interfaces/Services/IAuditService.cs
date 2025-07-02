using Application.Common;
using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces.Services;

/// <summary>
/// Interface for audit log services
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Creates a new audit log entry
    /// </summary>
    /// <param name="createDto">Audit log creation data</param>
    /// <returns>ID of the created audit log</returns>
    Task<int> CreateAuditLogAsync(CreateAuditLogDto createDto);
    
    /// <summary>
    /// Gets a paginated list of audit logs with optional filtering
    /// </summary>
    /// <param name="request">Pagination parameters</param>
    /// <param name="filter">Optional filter criteria</param>
    /// <returns>Paginated result of audit logs</returns>
    Task<PagedResult<AuditLogDto>> GetPaginatedAuditLogsAsync(PagedRequest request, AuditLogFilterDto? filter = null);
    
    /// <summary>
    /// Gets a specific audit log by ID
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <returns>The audit log details or null if not found</returns>
    Task<AuditLogDto?> GetAuditLogByIdAsync(int id);
    
    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">ID of entity</param>
    /// <returns>List of audit logs for the entity</returns>
    Task<List<AuditLogDto>> GetAuditLogsForEntityAsync(string entityType, string entityId);
    
    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of audit logs for the user</returns>
    Task<List<AuditLogDto>> GetAuditLogsByUserIdAsync(int userId);
    
    /// <summary>
    /// Gets a comprehensive audit log report
    /// </summary>
    /// <param name="filter">Optional filter criteria</param>
    /// <returns>An audit log report with statistics</returns>
    Task<AuditLogReportDto> GetAuditLogReportAsync(AuditLogFilterDto? filter = null);
    
    /// <summary>
    /// Gets audit logs for a specific action type
    /// </summary>
    /// <param name="actionType">Type of action</param>
    /// <returns>List of audit logs for the action type</returns>
    Task<List<AuditLogDto>> GetAuditLogsByActionTypeAsync(AuditActionType actionType);
    
    /// <summary>
    /// Gets audit logs for a specific date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of audit logs within the date range</returns>
    Task<List<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
}