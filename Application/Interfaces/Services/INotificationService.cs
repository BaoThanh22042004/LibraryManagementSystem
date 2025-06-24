using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPaginatedNotificationsAsync(PagedRequest request, string? searchTerm = null);
    Task<NotificationDto?> GetNotificationByIdAsync(int id);
    Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId);
    Task<int> CreateNotificationAsync(CreateNotificationDto notificationDto);
    Task UpdateNotificationAsync(int id, UpdateNotificationDto notificationDto);
    Task DeleteNotificationAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> MarkAsReadAsync(int id);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId);
    Task<int> GetUnreadNotificationCountAsync(int userId);
    Task<bool> SendOverdueNotificationsAsync();
    Task<bool> SendReservationAvailableNotificationsAsync();
}