namespace Domain.Enums;

/// <summary>
/// Represents the types of actions that can be audited in the system
/// </summary>
public enum AuditActionType
{
	/// <summary>
	/// Read an entity
    /// </summary>
	Read = 1,

	/// <summary>
	/// Creation of a new entity
	/// </summary>
	Create = 2,
    
    /// <summary>
    /// Update of an existing entity
    /// </summary>
    Update = 3,
    
    /// <summary>
    /// Deletion of an existing entity
    /// </summary>
    Delete = 4,
    
    /// <summary>
    /// Other actions not categorized above
    /// </summary>
    Other = 99
}