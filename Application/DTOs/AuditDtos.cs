using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request DTO for creating an audit log entry.
/// </summary>
public record CreateAuditLogRequest
{
    public int? UserId { get; set; }
    public AuditActionType ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? IpAddress { get; set; }
    public string? Module { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response DTO for audit log entries.
/// </summary>
public record AuditLogResponse
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? IpAddress { get; set; }
    public string? Module { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for searching audit logs.
/// </summary>
public record AuditLogSearchRequest : PagedRequest
{
    public int? UserId { get; set; }
    public AuditActionType? ActionType { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsSuccess { get; set; }
}

public record DashboardStatsDto
{
    public int TotalMembers { get; set; }
    public int TotalBooks { get; set; }
    public int TotalBookCopies { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int PendingFines { get; set; }
    public int Reservations { get; set; }
    public int NotificationsSent { get; set; }
    public DateTime StatsGeneratedAt { get; set; }
}