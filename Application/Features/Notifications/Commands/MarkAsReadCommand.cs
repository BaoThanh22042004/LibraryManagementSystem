using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

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
            
            // Mark as read - in this case, we'll delete the notification
            // Another approach could be to add a 'Read' status or a 'IsRead' flag to the entity
            notificationRepository.Delete(notification);
            
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