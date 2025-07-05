using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Services;

/// <summary>
/// Implementation of IAuditService that provides audit logging functionality.
/// Supports Business Rule: BR-22 (Audit Logging Requirement).
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task CreateAuditLogAsync(CreateAuditLogRequest request)
    {
        try
        {
            var auditLog = _mapper.Map<AuditLog>(request);
            
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Audit log created: {ActionType} on {EntityType} {EntityId}",
                auditLog.ActionType, auditLog.EntityType, auditLog.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for {ActionType} on {EntityType}", 
                request.ActionType, request.EntityType);
            
            // We don't want audit logging failures to break the application
            // Just log the error and continue
        }
    }

    /// <inheritdoc/>
    public async Task LogAuthenticationAsync(int? userId, AuditActionType action, bool isSuccess, string details, string? ipAddress = null, string? errorMessage = null)
    {
        var request = new CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = action,
            EntityType = "User",
            EntityId = userId?.ToString(),
            Details = details,
            IpAddress = ipAddress,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Module = "Authentication"
        };

        await CreateAuditLogAsync(request);
    }

    /// <inheritdoc/>
    public async Task LogUserManagementAsync(int performedByUserId, int targetUserId, AuditActionType action, bool isSuccess, string details, string? beforeState = null, string? afterState = null, string? ipAddress = null, string? errorMessage = null)
    {
        var request = new CreateAuditLogRequest
        {
            UserId = performedByUserId,
            ActionType = action,
            EntityType = "User",
            EntityId = targetUserId.ToString(),
            Details = details,
            BeforeState = beforeState,
            AfterState = afterState,
            IpAddress = ipAddress,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Module = "UserManagement"
        };

        await CreateAuditLogAsync(request);
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<AuditLogResponse>>> SearchAuditLogsAsync(AuditLogSearchRequest request)
    {
        try
        {
            // Building the filter predicate
            Expression<Func<AuditLog, bool>>? predicate = null;

            if (request.UserId.HasValue || 
                request.ActionType.HasValue || 
                !string.IsNullOrEmpty(request.EntityType) || 
                !string.IsNullOrEmpty(request.EntityId) || 
                !string.IsNullOrEmpty(request.SearchTerm) ||
                request.StartDate.HasValue ||
                request.EndDate.HasValue ||
                request.IsSuccess.HasValue)
            {
                predicate = log => (
                    (!request.UserId.HasValue || log.UserId == request.UserId) &&
                    (!request.ActionType.HasValue || log.ActionType == request.ActionType) &&
                    (string.IsNullOrEmpty(request.EntityType) || log.EntityType == request.EntityType) &&
                    (string.IsNullOrEmpty(request.EntityId) || log.EntityId == request.EntityId) &&
                    (string.IsNullOrEmpty(request.SearchTerm) || 
                     log.Details.Contains(request.SearchTerm) || 
                     (log.EntityName != null && log.EntityName.Contains(request.SearchTerm)) ||
                     (log.ErrorMessage != null && log.ErrorMessage.Contains(request.SearchTerm))) &&
                    (!request.StartDate.HasValue || log.CreatedAt >= request.StartDate) &&
                    (!request.EndDate.HasValue || log.CreatedAt <= request.EndDate.Value.AddDays(1).AddSeconds(-1)) &&
                    (!request.IsSuccess.HasValue || log.IsSuccess == request.IsSuccess)
                );
            }

            // Default sorting by most recent first
            Func<IQueryable<AuditLog>, IOrderedQueryable<AuditLog>> orderBy = 
                q => q.OrderByDescending(log => log.CreatedAt);

            var pagedRequest = new PagedRequest(request.PageNumber, request.PageSize);
            
            // Include the User navigation property to get user names
            var pagedAuditLogs = await _unitOfWork.Repository<AuditLog>()
                .PagedListAsync(
                    pagedRequest, 
                    predicate, 
                    orderBy, 
                    true, 
                    log => log.User!);

            // Map to response DTOs
            var auditLogResponses = _mapper.Map<List<AuditLogResponse>>(pagedAuditLogs.Items);
            
            var pagedResult = new PagedResult<AuditLogResponse>(
                auditLogResponses, 
                pagedAuditLogs.Count, 
                pagedAuditLogs.Page, 
                pagedAuditLogs.PageSize);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            return Result.Failure<PagedResult<AuditLogResponse>>("An error occurred while searching audit logs. Please try again later.");
        }
    }
}