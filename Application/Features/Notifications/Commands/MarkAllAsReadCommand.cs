using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to mark all notifications for a user as read.
/// </summary>
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
            
            // Get all user's unread notifications (those with Sent status)
            var notifications = await notificationRepository.ListAsync(
                predicate: n => n.UserId == request.UserId && n.Status == NotificationStatus.Sent
            );
            
            if (notifications.Count == 0)
                return Result.Success(); // No notifications to mark as read
            
            // Mark all as read by updating their status
            foreach (var notification in notifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.LastModifiedAt = DateTime.UtcNow;
                notificationRepository.Update(notification);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to mark all notifications as read: {ex.Message}");
        }
    }
}