using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Notifications.Queries;

/// <summary>
/// Query to get a paginated list of notifications with optional search functionality.
/// </summary>
public record GetPaginatedNotificationsQuery(PagedRequest PagedRequest, string? SearchTerm = null) : IRequest<PagedResult<NotificationDto>>;

public class GetPaginatedNotificationsQueryHandler : IRequestHandler<GetPaginatedNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaginatedNotificationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetPaginatedNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notificationRepository = _unitOfWork.Repository<Notification>();
        
        // Build search predicate if search term is provided
        Expression<Func<Notification, bool>>? predicate = null;
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = n => 
                n.Subject.ToLower().Contains(searchTerm) || 
                n.Message.ToLower().Contains(searchTerm) ||
                (n.User != null && n.User.FullName.ToLower().Contains(searchTerm));
        }
        
        // Get paginated notifications
        var pagedNotifications = await notificationRepository.PagedListAsync(
            pagedRequest: request.PagedRequest,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(n => n.SentAt ?? n.CreatedAt),
            asNoTracking: true,
            n => n.User!
        );
        
        // Map to DTOs
        var notificationDtos = _mapper.Map<List<NotificationDto>>(pagedNotifications.Items);
        
        // Set UserName for each notification
        for (int i = 0; i < pagedNotifications.Items.Count; i++)
        {
            notificationDtos[i].UserName = pagedNotifications.Items[i].User?.FullName;
        }
        
        return new PagedResult<NotificationDto>(
            notificationDtos,
            pagedNotifications.TotalCount,
            pagedNotifications.PageNumber,
            pagedNotifications.PageSize
        );
    }
}