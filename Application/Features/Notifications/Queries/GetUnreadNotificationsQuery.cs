using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Queries;

/// <summary>
/// Query to get all unread notifications for a specific user.
/// Implements UC036: View Unread Notifications
/// </summary>
public record GetUnreadNotificationsQuery(int UserId) : IRequest<List<NotificationDto>>;

public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, List<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUnreadNotificationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<NotificationDto>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        
        var unreadNotifications = await notificationRepository.ListAsync(
            predicate: n => n.UserId == request.UserId && n.Status == NotificationStatus.Sent,
            orderBy: q => q.OrderByDescending(n => n.SentAt ?? n.CreatedAt),
            asNoTracking: true,
            n => n.User!
        );
        
        var notificationDtos = _mapper.Map<List<NotificationDto>>(unreadNotifications);
        
        // Set UserName for each notification
        for (int i = 0; i < unreadNotifications.Count; i++)
        {
            notificationDtos[i].UserName = unreadNotifications[i].User?.FullName;
        }
        
        return notificationDtos;
    }
}