using Application.DTOs;
using Application.Common;

namespace Application.Interfaces;

public interface INotificationService
{
    Task<Common.Result<NotificationReadDto>> CreateNotificationAsync(NotificationCreateDto dto);
    Task<Common.Result<List<NotificationReadDto>>> CreateNotificationsBulkAsync(NotificationBatchCreateDto dto);
    Task<Common.Result<NotificationReadDto>> UpdateNotificationStatusAsync(NotificationUpdateStatusDto dto);
    Task<Common.Result<(int SuccessCount, List<(int Id, string Reason)> Failures)>> MarkAsReadAsync(NotificationMarkAsReadDto dto, int memberId);
    Task<Common.Result<List<NotificationListDto>>> GetNotificationsAsync(int memberId, bool unreadOnly = false);
    Task<Common.Result<NotificationReadDto>> GetNotificationDetailAsync(int notificationId, int requesterId, bool isStaff = false);
    Task<Common.Result<int>> GetUnreadCountAsync(int memberId);
    Task<Common.Result<(int SuccessCount, List<string> Errors)>> SendOverdueNotificationsAsync(); // For scheduled job
    Task<Common.Result<(int SuccessCount, List<string> Errors)>> SendAvailabilityNotificationsAsync(); // For scheduled job
}
