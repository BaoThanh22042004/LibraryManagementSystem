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

/// <summary>
/// Service for managing notification operations in the library system.
/// </summary>
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

    /// <summary>
    /// Gets a paginated list of notifications with optional search functionality.
    /// </summary>
    public async Task<PagedResult<NotificationDto>> GetPaginatedNotificationsAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetPaginatedNotificationsQuery(request, searchTerm));
    }

    /// <summary>
    /// Gets a specific notification by its ID.
    /// </summary>
    public async Task<NotificationDto?> GetNotificationByIdAsync(int id)
    {
        return await _mediator.Send(new GetNotificationByIdQuery(id));
    }

    /// <summary>
    /// Gets all notifications for a specific user.
    /// </summary>
    public async Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId)
    {
        return await _mediator.Send(new GetNotificationsByUserIdQuery(userId));
    }

    /// <summary>
    /// Creates a new notification in the system.
    /// </summary>
    public async Task<int> CreateNotificationAsync(CreateNotificationDto notificationDto)
    {
        var result = await _mediator.Send(new CreateNotificationCommand(notificationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    /// <summary>
    /// Updates a notification's status.
    /// </summary>
    public async Task UpdateNotificationAsync(int id, UpdateNotificationDto notificationDto)
    {
        var result = await _mediator.Send(new UpdateNotificationCommand(id, notificationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    /// <summary>
    /// Deletes a notification that is no longer needed.
    /// </summary>
    public async Task DeleteNotificationAsync(int id)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    /// <summary>
    /// Checks if a notification with the specified ID exists.
    /// </summary>
    public async Task<bool> ExistsAsync(int id)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        return await notificationRepository.ExistsAsync(n => n.Id == id);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int id)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(id));
        return result.IsSuccess;
    }

    /// <summary>
    /// Marks all notifications for a user as read.
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var result = await _mediator.Send(new MarkAllAsReadCommand(userId));
        return result.IsSuccess;
    }

    /// <summary>
    /// Gets all unread notifications for a specific user.
    /// </summary>
    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId)
    {
        return await _mediator.Send(new GetUnreadNotificationsQuery(userId));
    }

    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// </summary>
    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        return await _mediator.Send(new GetUnreadNotificationCountQuery(userId));
    }

    /// <summary>
    /// Sends notifications to users with overdue loans.
    /// </summary>
    public async Task<bool> SendOverdueNotificationsAsync()
    {
        var result = await _mediator.Send(new SendOverdueNotificationsCommand());
        return result.IsSuccess;
    }

    /// <summary>
    /// Sends notifications to users when their reserved books become available.
    /// </summary>
    public async Task<bool> SendReservationAvailableNotificationsAsync()
    {
        var result = await _mediator.Send(new SendReservationAvailableNotificationsCommand());
        return result.IsSuccess;
    }
}