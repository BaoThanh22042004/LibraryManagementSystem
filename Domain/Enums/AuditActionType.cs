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
    /// User login action
    /// </summary>
    Login = 5,
    
    /// <summary>
    /// User logout action
    /// </summary>
    Logout = 6,
    
    /// <summary>
    /// Password change action
    /// </summary>
    PasswordChange = 7,
    
    /// <summary>
    /// Password reset action
    /// </summary>
    PasswordReset = 8,
    
    /// <summary>
    /// User registration action
    /// </summary>
    Register = 9,
    
    /// <summary>
    /// Book loan action
    /// </summary>
    Loan = 10,
    
    /// <summary>
    /// Book return action
    /// </summary>
    Return = 11,
    
    /// <summary>
    /// Book reservation action
    /// </summary>
    Reserve = 12,
    
    /// <summary>
    /// Fine payment action
    /// </summary>
    PayFine = 13,
    
    /// <summary>
    /// Other actions not categorized above
    /// </summary>
    Other = 99
}