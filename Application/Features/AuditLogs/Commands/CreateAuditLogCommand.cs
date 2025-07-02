using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.AuditLogs.Commands;

/// <summary>
/// Command to create a new audit log entry
/// </summary>
public record CreateAuditLogCommand(CreateAuditLogDto AuditLogDto) : IRequest<Result<int>>;

/// <summary>
/// Handler for creating a new audit log entry
/// </summary>
public class CreateAuditLogCommandHandler : IRequestHandler<CreateAuditLogCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAuditLogCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateAuditLogCommand request, CancellationToken cancellationToken)
    {
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        try
        {
            var auditLog = new AuditLog
            {
                UserId = request.AuditLogDto.UserId,
                ActionType = request.AuditLogDto.ActionType,
                EntityType = request.AuditLogDto.EntityType,
                EntityId = request.AuditLogDto.EntityId,
                EntityName = request.AuditLogDto.EntityName,
                Details = request.AuditLogDto.Details,
                BeforeState = request.AuditLogDto.BeforeState,
                AfterState = request.AuditLogDto.AfterState,
                IpAddress = request.AuditLogDto.IpAddress,
                Module = request.AuditLogDto.Module,
                IsSuccess = request.AuditLogDto.IsSuccess,
                ErrorMessage = request.AuditLogDto.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };
            
            await auditLogRepository.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Success(auditLog.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Failed to create audit log: {ex.Message}");
        }
    }
}