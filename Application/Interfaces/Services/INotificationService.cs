using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

/// <summary>
/// Interface for notification management service.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets a paginated list of notifications with optional search functionality.
    /// </summary>
    Task<PagedResult<NotificationDto>> GetPaginatedNotificationsAsync(PagedRequest request, string? searchTerm = null);
    
    /// <summary>
    /// Gets a specific notification by its ID.
    /// </summary>
    Task<NotificationDto?> GetNotificationByIdAsync(int id);
    
    /// <summary>
    /// Gets all notifications for a specific user.
    /// </summary>
    Task<List<NotificationDto>> GetNotificationsByUserIdAsync(int userId);
    
    /// <summary>
    /// Creates a new notification in the system.
    /// </summary>
    Task<int> CreateNotificationAsync(CreateNotificationDto notificationDto);
    
    /// <summary>
    /// Updates a notification's status.
    /// </summary>
    Task UpdateNotificationAsync(int id, UpdateNotificationDto notificationDto);
    
    /// <summary>
    /// Deletes a notification that is no longer needed.
    /// </summary>
    Task DeleteNotificationAsync(int id);
    
    /// <summary>
    /// Checks if a notification with the specified ID exists.
    /// </summary>
    Task<bool> ExistsAsync(int id);
    
    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(int id);
    
    /// <summary>
    /// Marks all notifications for a user as read.
    /// </summary>
    Task<bool> MarkAllAsReadAsync(int userId);
    
    /// <summary>
    /// Gets all unread notifications for a specific user.
    /// </summary>
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId);
    
    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// </summary>
    Task<int> GetUnreadNotificationCountAsync(int userId);
    
    /// <summary>
    /// Sends notifications to users with overdue loans.
    /// </summary>
    Task<bool> SendOverdueNotificationsAsync();
    
    /// <summary>
    /// Sends notifications to users when their reserved books become available.
    /// </summary>
    Task<bool> SendReservationAvailableNotificationsAsync();
}