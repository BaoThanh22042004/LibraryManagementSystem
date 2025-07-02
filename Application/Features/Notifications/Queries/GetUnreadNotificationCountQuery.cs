using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Queries;

/// <summary>
/// Query to get the count of unread notifications for a specific user.
/// </summary>
public record GetUnreadNotificationCountQuery(int UserId) : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUnreadNotificationCountQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        
        return await notificationRepository.CountAsync(
            n => n.UserId == request.UserId && n.Status == NotificationStatus.Sent
        );
    }
}