using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to mark all notifications for a user as read (UC032 - Mark as Read).
/// </summary>
/// <remarks>
/// This implementation follows UC032 specifications:
/// - Validates member has unread notifications to process
/// - Updates all notifications to "read" status in a batch operation
/// - Records read timestamp for tracking purposes
/// - Preserves notification records for historical reference
/// - Handles errors gracefully for partial success in batch operations
/// </remarks>
public record MarkAllAsReadCommand(int UserId) : IRequest<Result>;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var notificationRepository = _unitOfWork.Repository<Notification>();
            var userRepository = _unitOfWork.Repository<User>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Verify the user exists (PRE-1: Member must be authenticated)
            var user = await userRepository.GetAsync(u => u.Id == request.UserId);
            if (user == null)
                return Result.Failure($"User with ID {request.UserId} not found.");
                
            // Get all user's unread notifications (PRE-2: Member must have unread notifications)
            var notifications = await notificationRepository.ListAsync(
                predicate: n => n.UserId == request.UserId && n.Status == NotificationStatus.Sent
            );
            
            if (notifications.Count == 0)
                return Result.Success($"No unread notifications found for user {request.UserId}."); // Alternative flow 32.3: No Unread Notifications
            
            int successCount = 0;
            List<string> errors = new();
            
            // Mark all as read by updating their status (POST-1, POST-2: Notifications marked as read with timestamp)
            foreach (var notification in notifications)
            {
                try {
                    notification.Status = NotificationStatus.Read;
                    notification.LastModifiedAt = DateTime.UtcNow; // Records read timestamp
                    notificationRepository.Update(notification);
                    successCount++;
                }
                catch (Exception ex) {
                    // Handle partial failures (UC032.E4: Partial Failure)
                    errors.Add($"Notification ID {notification.Id}: {ex.Message}");
                }
            }
            
            // Record batch action in audit log
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Notification",
                EntityId = request.UserId.ToString(),
                EntityName = $"User Notifications",
                ActionType = AuditActionType.Update,
                Details = $"Marked {successCount} notifications as read for user: {user.FullName}",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            if (errors.Count > 0)
                return Result.Success($"Marked {successCount} notifications as read. {errors.Count} notifications failed: {string.Join("; ", errors)}");
                
            return Result.Success($"Successfully marked {successCount} notifications as read."); // POST-4: System confirms successful operation
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to mark notifications as read: {ex.Message}"); // UC032.E3: System Error
        }
    }
}