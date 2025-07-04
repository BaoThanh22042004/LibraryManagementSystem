namespace Domain.Enums;

/// <summary>
/// Represents the status of a fine imposed on a member.
/// Used in fine management (UC026–UC029).
/// </summary>
public enum FineStatus
{
	/// <summary>
	/// The fine is pending and has not yet been paid or waived.
	/// </summary>
	Pending = 1,
	/// <summary>
	/// The fine has been paid by the member.
	/// </summary>
	Paid = 2,
	/// <summary>
	/// The fine has been waived by staff.
	/// </summary>
	Waived = 3
}
