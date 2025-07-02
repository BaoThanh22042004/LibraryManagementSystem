using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Notifications.Queries;

/// <summary>
/// Query to get all notifications for a specific user.
/// </summary>
public record GetNotificationsByUserIdQuery(int UserId) : IRequest<List<NotificationDto>>;

public class GetNotificationsByUserIdQueryHandler : IRequestHandler<GetNotificationsByUserIdQuery, List<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetNotificationsByUserIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        
        var notifications = await notificationRepository.ListAsync(
            predicate: n => n.UserId == request.UserId,
            orderBy: q => q.OrderByDescending(n => n.SentAt ?? n.CreatedAt),
            asNoTracking: true,
            n => n.User!
        );
        
        var notificationDtos = _mapper.Map<List<NotificationDto>>(notifications);
        
        // Set UserName for each notification
        for (int i = 0; i < notifications.Count; i++)
        {
            notificationDtos[i].UserName = notifications[i].User?.FullName;
        }
        
        // Record the access for audit purposes
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        if (user != null)
        {
            user.LastModifiedAt = DateTime.UtcNow;
            userRepository.Update(user);
            await userRepository.SaveChangesAsync();
        }
        
        return notificationDtos;
    }
}