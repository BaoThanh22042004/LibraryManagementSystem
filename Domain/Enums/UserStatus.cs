namespace Domain.Enums;

/// <summary>
/// Represents the status of a user account in the system.
/// Used in user lifecycle and account management (UC001–UC009).
/// </summary>
public enum UserStatus
{
	/// <summary>
	/// The user has registered but not yet verified their email or account.
	/// </summary>
	UnVerified = 1,
	/// <summary>
	/// The user account is active and can access the system.
	/// </summary>
	Active = 2,
	/// <summary>
	/// The user account is suspended (temporarily disabled).
	/// </summary>
	Suspended = 3,
	/// <summary>
	/// The user account is inactive (permanently disabled or deleted).
	/// </summary>
	Inactive = 4
}
