using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Notifications.Commands;

public record DeleteNotificationCommand(int Id) : IRequest<Result>;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var notificationRepository = _unitOfWork.Repository<Notification>();
            
            // Get notification
            var notification = await notificationRepository.GetAsync(n => n.Id == request.Id);
            if (notification == null)
                return Result.Failure($"Notification with ID {request.Id} not found.");
            
            // Delete notification
            notificationRepository.Delete(notification);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to delete notification: {ex.Message}");
        }
    }
}