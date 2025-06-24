using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Notifications.Queries;

public record GetNotificationByIdQuery(int Id) : IRequest<NotificationDto?>;

public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, NotificationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetNotificationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<NotificationDto?> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        
        var notification = await notificationRepository.GetAsync(
            n => n.Id == request.Id,
            n => n.User!
        );
        
        if (notification == null)
            return null;
        
        var notificationDto = _mapper.Map<NotificationDto>(notification);
        
        // Set UserName if user is available
        notificationDto.UserName = notification.User?.FullName;
        
        return notificationDto;
    }
}