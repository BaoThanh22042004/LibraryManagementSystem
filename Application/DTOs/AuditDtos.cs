using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Request DTO for creating an audit log entry.
/// </summary>
public class CreateAuditLogRequest
{
    public int? UserId { get; set; }
    public AuditActionType ActionType { get; set; }
    
    [Required(ErrorMessage = "Entity type is required")]
    public string EntityType { get; set; } = string.Empty;
    
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    
    [Required(ErrorMessage = "Details are required")]
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
public class AuditLogResponse
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
public class AuditLogSearchRequest
{
    public int? UserId { get; set; }
    public AuditActionType? ActionType { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsSuccess { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
