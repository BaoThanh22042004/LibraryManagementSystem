using Application.Common;
using Application.DTOs;
using Application.Features.AuditLogs.Commands;
using Application.Features.AuditLogs.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Services;

/// <summary>
/// Service for managing audit logs in the library system
/// </summary>
public class AuditService : IAuditService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new audit log entry
    /// </summary>
    public async Task<int> CreateAuditLogAsync(CreateAuditLogDto createDto)
    {
        var result = await _mediator.Send(new CreateAuditLogCommand(createDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    /// <summary>
    /// Gets a paginated list of audit logs with optional filtering
    /// </summary>
    public async Task<PagedResult<AuditLogDto>> GetPaginatedAuditLogsAsync(PagedRequest request, AuditLogFilterDto? filter = null)
    {
        return await _mediator.Send(new GetPaginatedAuditLogsQuery(request, filter));
    }

    /// <summary>
    /// Gets a specific audit log by ID
    /// </summary>
    public async Task<AuditLogDto?> GetAuditLogByIdAsync(int id)
    {
        return await _mediator.Send(new GetAuditLogByIdQuery(id));
    }

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditLogsForEntityAsync(string entityType, string entityId)
    {
        return await _mediator.Send(new GetAuditLogsForEntityQuery(entityType, entityId));
    }

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditLogsByUserIdAsync(int userId)
    {
        return await _mediator.Send(new GetAuditLogsByUserIdQuery(userId));
    }

    /// <summary>
    /// Gets a comprehensive audit log report
    /// </summary>
    public async Task<AuditLogReportDto> GetAuditLogReportAsync(AuditLogFilterDto? filter = null)
    {
        return await _mediator.Send(new GetAuditLogReportQuery(filter));
    }

    /// <summary>
    /// Gets audit logs for a specific action type
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditLogsByActionTypeAsync(AuditActionType actionType)
    {
        return await _mediator.Send(new GetAuditLogsByActionTypeQuery(actionType));
    }

    /// <summary>
    /// Gets audit logs for a specific date range
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _mediator.Send(new GetAuditLogsByDateRangeQuery(startDate, endDate));
    }
}