using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for audit log entries
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Name of the user who performed the action
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Type of action performed
    /// </summary>
    public AuditActionType ActionType { get; set; }
    
    /// <summary>
    /// Display name for the action type
    /// </summary>
    public string ActionName => ActionType.ToString();
    
    /// <summary>
    /// Entity type affected by the action
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity affected by the action
    /// </summary>
    public string? EntityId { get; set; }
    
    /// <summary>
    /// Human-readable name of the affected entity
    /// </summary>
    public string? EntityName { get; set; }
    
    /// <summary>
    /// Details about the performed action
    /// </summary>
    public string Details { get; set; } = string.Empty;
    
    /// <summary>
    /// Entity state before the action
    /// </summary>
    public string? BeforeState { get; set; }
    
    /// <summary>
    /// Entity state after the action
    /// </summary>
    public string? AfterState { get; set; }
    
    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Module or area where the action occurred
    /// </summary>
    public string? Module { get; set; }
    
    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for filtering audit logs in search queries
/// </summary>
public class AuditLogFilterDto
{
    /// <summary>
    /// Filter by user ID
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Filter by action types
    /// </summary>
    public AuditActionType[]? ActionTypes { get; set; }
    
    /// <summary>
    /// Filter by entity type
    /// </summary>
    public string? EntityType { get; set; }
    
    /// <summary>
    /// Filter by entity ID
    /// </summary>
    public string? EntityId { get; set; }
    
    /// <summary>
    /// Filter by start date
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Filter by end date
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Filter by success status
    /// </summary>
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// Search term to look for in details and entity names
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Filter by module or area
    /// </summary>
    public string? Module { get; set; }
}

/// <summary>
/// DTO for creating a new audit log entry
/// </summary>
public class CreateAuditLogDto
{
    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Type of action performed
    /// </summary>
    public AuditActionType ActionType { get; set; }
    
    /// <summary>
    /// Entity type affected by the action
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity affected by the action
    /// </summary>
    public string? EntityId { get; set; }
    
    /// <summary>
    /// Human-readable name of the affected entity
    /// </summary>
    public string? EntityName { get; set; }
    
    /// <summary>
    /// Details about the performed action
    /// </summary>
    public string Details { get; set; } = string.Empty;
    
    /// <summary>
    /// Entity state before the action
    /// </summary>
    public string? BeforeState { get; set; }
    
    /// <summary>
    /// Entity state after the action
    /// </summary>
    public string? AfterState { get; set; }
    
    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Module or area where the action occurred
    /// </summary>
    public string? Module { get; set; }
    
    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for audit log report
/// </summary>
public class AuditLogReportDto
{
    /// <summary>
    /// List of audit log entries in the report
    /// </summary>
    public List<AuditLogDto> AuditLogs { get; set; } = [];
    
    /// <summary>
    /// Total number of audit logs matching the criteria
    /// </summary>
    public int TotalCount => AuditLogs.Count;
    
    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Filter criteria used to generate the report
    /// </summary>
    public AuditLogFilterDto? FilterCriteria { get; set; }
    
    /// <summary>
    /// Count of audit logs by action type
    /// </summary>
    public Dictionary<AuditActionType, int> ActionTypeCounts { get; set; } = new();
    
    /// <summary>
    /// Count of audit logs by entity type
    /// </summary>
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    
    /// <summary>
    /// Count of successful vs. failed actions
    /// </summary>
    public Dictionary<bool, int> SuccessFailureCounts { get; set; } = new();
}