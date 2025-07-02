using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to update a notification's status.
/// </summary>
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
            
            // Get notification
            var notification = await notificationRepository.GetAsync(n => n.Id == request.Id);
            if (notification == null)
                return Result.Failure($"Notification with ID {request.Id} not found.");
            
            // Validate status transition
            if (notification.Status == NotificationStatus.Sent && request.NotificationDto.Status == NotificationStatus.Pending)
                return Result.Failure("Cannot change status from Sent back to Pending.");
            
            // Update notification status
            notification.Status = request.NotificationDto.Status;
            notification.LastModifiedAt = DateTime.UtcNow;
            
            // If status is changed to Sent, update the SentAt timestamp
            if (notification.Status == NotificationStatus.Sent && notification.SentAt == null)
            {
                notification.SentAt = DateTime.UtcNow;
            }
            
            notificationRepository.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update notification: {ex.Message}");
        }
    }
}