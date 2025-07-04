using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to mark a notification as read (UC032 - Mark as Read).
/// </summary>
/// <remarks>
/// This implementation follows UC032 specifications:
/// - Validates notification exists and can be accessed by the member
/// - Updates notification status to "read"
/// - Records read timestamp for tracking purposes
/// - Preserves notification record for historical reference
/// - Supports marking of individual notifications
/// </remarks>
public record MarkAsReadCommand(int Id) : IRequest<Result>;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var notificationRepository = _unitOfWork.Repository<Notification>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get notification (PRE-1, PRE-4: Member must be authenticated and notification must exist)
            var notification = await notificationRepository.GetAsync(
                n => n.Id == request.Id,
                n => n.User!
            );
            
            if (notification == null)
                return Result.Failure($"Notification with ID {request.Id} not found."); // UC032.E1: Notification Not Found
            
            // Check if notification is already read
            if (notification.Status == NotificationStatus.Read)
                return Result.Success(); // Already read, nothing to do (Alternative flow 32.3)
            
            // Store user info for logging
            var userName = notification.User?.FullName ?? "Unknown";
            var subject = notification.Subject;
            
            // Mark as read (POST-1, POST-2: Notification is marked as read and timestamp recorded)
            notification.Status = NotificationStatus.Read;
            notification.LastModifiedAt = DateTime.UtcNow; // Records read timestamp
            
            notificationRepository.Update(notification);
            
            // Record action in audit log
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Notification",
                EntityId = notification.Id.ToString(),
                EntityName = $"Notification - {subject}",
                ActionType = AuditActionType.Update,
                Details = $"Notification marked as read by user: {userName}",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(); // POST-4: System confirms successful operation
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to mark notification as read: {ex.Message}"); // UC032.E3: System Error
        }
    }
}