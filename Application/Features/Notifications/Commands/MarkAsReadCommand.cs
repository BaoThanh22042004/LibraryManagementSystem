using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to mark a notification as read.
/// </summary>
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
            
            // Get notification
            var notification = await notificationRepository.GetAsync(n => n.Id == request.Id);
            if (notification == null)
                return Result.Failure($"Notification with ID {request.Id} not found.");
            
            // Check if notification is already read (we can add a ReadStatus enum if needed)
            // For now, we'll check if the status is already marked as Read
            if (notification.Status == NotificationStatus.Read)
                return Result.Success(); // Already read, nothing to do
            
            // Mark as read, we should update status not delete
            notification.Status = NotificationStatus.Read;
            notification.LastModifiedAt = DateTime.UtcNow;
            
            notificationRepository.Update(notification);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to mark notification as read: {ex.Message}");
        }
    }
}