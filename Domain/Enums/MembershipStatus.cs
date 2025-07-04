namespace Domain.Enums;

/// <summary>
/// Represents the status of a library membership.
/// Used in membership management (UC001, UC008, UC006, etc.).
/// </summary>
public enum MembershipStatus
{
	/// <summary>
	/// The membership is active and in good standing.
	/// </summary>
	Active = 1,
	/// <summary>
	/// The membership is suspended (temporarily disabled).
	/// </summary>
	Suspended = 2,
	/// <summary>
	/// The membership has expired and is no longer active.
	/// </summary>
	Expired = 3
}
