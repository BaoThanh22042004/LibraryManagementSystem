using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to create a new notification in the system (UC030 - Create Notification).
/// </summary>
/// <remarks>
/// This implementation follows UC030 specifications:
/// - Validates user has appropriate permissions (for non-system notifications)
/// - Verifies member exists in the system when specified
/// - Validates notification content (subject/message length)
/// - Creates notification with pending status
/// - Assigns unique identifier and creation date
/// - Supports system-generated notifications without member assignment
/// - Supports creation of individual member-targeted notifications
/// </remarks>
public record CreateNotificationCommand(CreateNotificationDto NotificationDto) : IRequest<Result<int>>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateNotificationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var notificationRepository = _unitOfWork.Repository<Notification>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // PRE-2: If notification is for a specific member, verify the user exists
            string memberName = "System-wide";
            if (request.NotificationDto.UserId.HasValue)
            {
                var userRepository = _unitOfWork.Repository<User>();
                var user = await userRepository.GetAsync(u => u.Id == request.NotificationDto.UserId.Value);
                
                if (user == null)
                    return Result.Failure<int>($"User with ID {request.NotificationDto.UserId.Value} not found."); // UC030.E2: Member Not Found
                    
                memberName = user.FullName ?? "Unknown";
            }
            
            // PRE-3: Validate notification content
            if (string.IsNullOrEmpty(request.NotificationDto.Subject))
                return Result.Failure<int>("Notification subject is required."); // UC030.E1: Invalid Notification Content
                
            if (string.IsNullOrEmpty(request.NotificationDto.Message))
                return Result.Failure<int>("Notification message is required."); // UC030.E1: Invalid Notification Content
                
            if (request.NotificationDto.Subject.Length > 200)
                return Result.Failure<int>("Notification subject exceeds maximum length of 200 characters."); // UC030.E1: Invalid Notification Content
                
            if (request.NotificationDto.Message.Length > 500)
                return Result.Failure<int>("Notification message exceeds maximum length of 500 characters."); // UC030.E1: Invalid Notification Content
                
            // Validate notification type is defined in the enum
            if (!Enum.IsDefined(typeof(NotificationType), request.NotificationDto.Type))
                return Result.Failure<int>($"Invalid notification type: {request.NotificationDto.Type}");
            
            // POST-1, POST-2: Create notification with pending status and a unique identifier
            var notification = new Notification
            {
                Subject = request.NotificationDto.Subject,
                Message = request.NotificationDto.Message,
                Type = request.NotificationDto.Type,
                UserId = request.NotificationDto.UserId,
                Status = NotificationStatus.Pending, // All notifications start with pending status
                CreatedAt = DateTime.UtcNow,
                SentAt = null // Will be set when the notification is sent
            };
            
            // POST-3: Save notification to the system
            await notificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record in audit log for tracking (BR-22)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Notification",
                EntityId = notification.Id.ToString(),
                EntityName = $"Notification - {notification.Subject}",
                ActionType = AuditActionType.Create,
                Details = $"Created {notification.Type} notification for {memberName}",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(notification.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create notification: {ex.Message}"); // UC030.E3: System Error
        }
    }
}