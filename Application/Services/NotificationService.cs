using Application.Common;
using Application.DTOs;
using Application.Features.Notifications.Commands;
using Application.Features.Notifications.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class NotificationService : INotificationService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<NotificationDto>> GetPaginatedNotificationsAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetPaginatedNotificationsQuery(request, searchTerm));
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(int id)
    {
        return await _mediator.Send(new GetNotificationByIdQuery(id));
    }

    public async Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId)
    {
        return await _mediator.Send(new GetNotificationsByUserIdQuery(userId));
    }

    public async Task<int> CreateNotificationAsync(CreateNotificationDto notificationDto)
    {
        var result = await _mediator.Send(new CreateNotificationCommand(notificationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateNotificationAsync(int id, UpdateNotificationDto notificationDto)
    {
        var result = await _mediator.Send(new UpdateNotificationCommand(id, notificationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteNotificationAsync(int id)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        return await notificationRepository.ExistsAsync(n => n.Id == id);
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(id));
        return result.IsSuccess;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var result = await _mediator.Send(new MarkAllAsReadCommand(userId));
        return result.IsSuccess;
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId)
    {
        return await _mediator.Send(new GetUnreadNotificationsQuery(userId));
    }

    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        return await _mediator.Send(new GetUnreadNotificationCountQuery(userId));
    }

    public async Task<bool> SendOverdueNotificationsAsync()
    {
        var result = await _mediator.Send(new SendOverdueNotificationsCommand());
        return result.IsSuccess;
    }

    public async Task<bool> SendReservationAvailableNotificationsAsync()
    {
        var result = await _mediator.Send(new SendReservationAvailableNotificationsCommand());
        return result.IsSuccess;
    }
}