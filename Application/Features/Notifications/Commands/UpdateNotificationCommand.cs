using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to update a notification's status (UC031 - Update Notification).
/// </summary>
/// <remarks>
/// This implementation follows UC031 specifications:
/// - Validates notification exists in the system
/// - Verifies new status is valid
/// - Updates notification status
/// - Sets delivery timestamp if status is changed to sent
/// - Preserves original delivery timestamp for already sent notifications
/// - Logs status change for audit purposes
/// </remarks>
public record UpdateNotificationCommand(int Id, UpdateNotificationDto NotificationDto) : IRequest<Result>;

public class UpdateNotificationCommandHandler : IRequestHandler<UpdateNotificationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var notificationRepository = _unitOfWork.Repository<Notification>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get notification (PRE-1: Notification must exist in the system)
            var notification = await notificationRepository.GetAsync(n => n.Id == request.Id, n => n.User!);
            if (notification == null)
                return Result.Failure($"Notification with ID {request.Id} not found."); // UC031.E1: Notification Not Found
            
            // Store original status for logging
            var oldStatus = notification.Status;
            
            // Validate status transition (PRE-2: New status must be valid)
            if (!IsValidStatusTransition(oldStatus, request.NotificationDto.Status))
                return Result.Failure($"Invalid status transition from {oldStatus} to {request.NotificationDto.Status}."); // UC031.E2: Invalid Status Value
            
            // Update notification status (POST-1: Notification status is updated in the system)
            notification.Status = request.NotificationDto.Status;
            notification.LastModifiedAt = DateTime.UtcNow;
            
            // If status is changed to Sent, update the SentAt timestamp (POST-2: Delivery timestamp is set if status becomes sent)
            // Alternative flow 31.1: Already Sent Notification - preserves original timestamp
            if (notification.Status == NotificationStatus.Sent && notification.SentAt == null)
            {
                notification.SentAt = DateTime.UtcNow;
            }
            
            notificationRepository.Update(notification);
            
            // Record status change in audit log (POST-4: Update activity is logged for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Notification",
                EntityId = notification.Id.ToString(),
                EntityName = $"Notification - {notification.Subject}",
                ActionType = AuditActionType.Update,
                Details = $"Status updated from {oldStatus} to {notification.Status}. " +
                          $"For user: {notification.User?.FullName ?? "System-wide"}",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(); // POST-3: Status change is saved permanently
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update notification: {ex.Message}"); // UC031.E3: System Error
        }
    }
    
    // Helper method to validate status transitions
    private bool IsValidStatusTransition(NotificationStatus oldStatus, NotificationStatus newStatus)
    {
        // Prevent going back from Sent to Pending
        if (oldStatus == NotificationStatus.Sent && newStatus == NotificationStatus.Pending)
            return false;
            
        // Prevent marking a failed notification as sent without retry
        if (oldStatus == NotificationStatus.Failed && newStatus == NotificationStatus.Sent)
            return false;
            
        // Allow other transitions
        return true;
    }
}