using Application.Common;
using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Service interface for audit logging operations.
/// Supports Business Rule: BR-22 (Audit Logging Requirement).
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Creates an audit log entry.
    /// </summary>
    /// <param name="request">Audit log entry data</param>
    /// <returns>Task completion</returns>
    Task CreateAuditLogAsync(CreateAuditLogRequest request);
    
    /// <summary>
    /// Logs user authentication actions.
    /// </summary>
    /// <param name="userId">User ID (null for failed logins with unknown user)</param>
    /// <param name="action">Authentication action type</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="details">Additional details</param>
    /// <param name="ipAddress">User's IP address</param>
    /// <param name="errorMessage">Error message if the action failed</param>
    /// <returns>Task completion</returns>
    Task LogAuthenticationAsync(int? userId, AuditActionType action, bool isSuccess, string details, string? ipAddress = null, string? errorMessage = null);
    
    /// <summary>
    /// Logs user management actions.
    /// </summary>
    /// <param name="performedByUserId">ID of the user performing the action</param>
    /// <param name="targetUserId">ID of the user being acted upon</param>
    /// <param name="action">User management action type</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="details">Additional details</param>
    /// <param name="beforeState">State before the action (optional)</param>
    /// <param name="afterState">State after the action (optional)</param>
    /// <param name="ipAddress">User's IP address</param>
    /// <param name="errorMessage">Error message if the action failed</param>
    /// <returns>Task completion</returns>
    Task LogUserManagementAsync(int performedByUserId, int targetUserId, AuditActionType action, bool isSuccess, string details, string? beforeState = null, string? afterState = null, string? ipAddress = null, string? errorMessage = null);
    
    /// <summary>
    /// Searches for audit logs based on criteria.
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <returns>Result with paged list of audit logs if successful</returns>
    Task<Result<PagedResult<AuditLogResponse>>> SearchAuditLogsAsync(AuditLogSearchRequest request);
}
