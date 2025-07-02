using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a record of a significant action in the system for audit and compliance purposes.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// The user who performed the action, if available
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// The type of action performed (e.g., Create, Update, Delete, Login)
    /// </summary>
    public AuditActionType ActionType { get; set; }
    
    /// <summary>
    /// The entity type that was affected by the action
    /// </summary>
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the entity that was affected, if applicable
    /// </summary>
    public string? EntityId { get; set; }
    
    /// <summary>
    /// The name or description of the entity for human readability
    /// </summary>
    [MaxLength(255)]
    public string? EntityName { get; set; }
    
    /// <summary>
    /// Details about the action that was performed
    /// </summary>
    [MaxLength(1000)]
    public string Details { get; set; } = string.Empty;
    
    /// <summary>
    /// The before state of the entity (JSON or description)
    /// </summary>
    public string? BeforeState { get; set; }
    
    /// <summary>
    /// The after state of the entity (JSON or description)
    /// </summary>
    public string? AfterState { get; set; }
    
    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// The application module or area where the action occurred
    /// </summary>
    [MaxLength(100)]
    public string? Module { get; set; }
    
    /// <summary>
    /// Success or failure status of the action
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Optional error message if the action failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}