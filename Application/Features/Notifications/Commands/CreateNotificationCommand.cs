using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

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
            
            // If UserId is provided, verify the user exists
            if (request.NotificationDto.UserId.HasValue)
            {
                var userRepository = _unitOfWork.Repository<User>();
                var userExists = await userRepository.ExistsAsync(u => u.Id == request.NotificationDto.UserId.Value);
                
                if (!userExists)
                    return Result.Failure<int>($"User with ID {request.NotificationDto.UserId.Value} not found.");
            }
            
            // Create notification
            var notification = new Notification
            {
                Subject = request.NotificationDto.Subject,
                Message = request.NotificationDto.Message,
                Type = request.NotificationDto.Type,
                UserId = request.NotificationDto.UserId,
                Status = NotificationStatus.Pending,
                SentAt = null // Will be set when the notification is sent
            };
            
            await notificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(notification.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create notification: {ex.Message}");
        }
    }
}