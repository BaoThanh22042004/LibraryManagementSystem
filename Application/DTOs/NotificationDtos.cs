using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class NotificationCreateDto
{
    public int? UserId { get; set; } // Nullable for system-wide
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class NotificationUpdateStatusDto
{
    public int NotificationId { get; set; }
    public NotificationStatus Status { get; set; }
}

public class NotificationMarkAsReadDto
{
    public List<int> NotificationIds { get; set; } = new();
}

public class NotificationReadDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? UserName { get; set; } // For staff view
}

public class NotificationListDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => Status == NotificationStatus.Read;
}

public class NotificationBatchCreateDto
{
    public List<int> UserIds { get; set; } = new();
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
