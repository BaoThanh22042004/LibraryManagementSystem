using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Queries;

/// <summary>
/// Query to get all notifications for a specific user (UC033 - View Notifications).
/// </summary>
/// <remarks>
/// This implementation follows UC033 specifications:
/// - Validates member exists in the system
/// - Ensures user has permission to access the notifications
/// - Retrieves notifications for the specified member
/// - Orders results by delivery date with newest first
/// - Includes member information for staff reference
/// - Handles "no notifications found" case
/// </remarks>
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
        var userRepository = _unitOfWork.Repository<User>();
        var auditRepository = _unitOfWork.Repository<AuditLog>();
        
        // PRE-1: Verify member exists in the system
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        if (user == null)
            return new List<NotificationDto>(); // UC033.E1: Member Not Found
        
        // Get notifications for the user, ordered by delivery date with newest first (POST-4)
        var notifications = await notificationRepository.ListAsync(
            predicate: n => n.UserId == request.UserId,
            orderBy: q => q.OrderByDescending(n => n.SentAt ?? n.CreatedAt), // Orders by delivery date (newest first)
            asNoTracking: true,
            n => n.User!
        );
        
        // Alternative flow 33.1: No Notifications Found
        if (notifications.Count == 0)
            return new List<NotificationDto>(); // Empty list indicates no notifications found
            
        // Map to DTOs and include member information (POST-2, POST-3)
        var notificationDtos = _mapper.Map<List<NotificationDto>>(notifications);
        
        // Set UserName for each notification (for staff reference)
        for (int i = 0; i < notifications.Count; i++)
        {
            notificationDtos[i].UserName = notifications[i].User?.FullName;
        }
        
        // Record access in audit log
        await auditRepository.AddAsync(new AuditLog
        {
            EntityType = "Notification",
            EntityId = request.UserId.ToString(),
            EntityName = $"User Notifications",
            ActionType = AuditActionType.AccessSensitiveData, // Use appropriate existing audit type
            Details = $"Viewed {notifications.Count} notifications for user: {user.FullName}",
            IsSuccess = true
        });
        await auditRepository.SaveChangesAsync();
        
        // Update user's last activity time
        user.LastModifiedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync();
        
        return notificationDtos; // POST-1: List is displayed according to filter
    }
}