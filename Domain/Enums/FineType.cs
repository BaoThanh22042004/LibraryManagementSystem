namespace Domain.Enums;

/// <summary>
/// Represents the type of fine imposed on a member.
/// Used in fine management (UC026–UC029).
/// </summary>
public enum FineType
{
	/// <summary>
	/// Fine for overdue book returns.
	/// </summary>
	Overdue = 1,
	/// <summary>
	/// Fine for lost books.
	/// </summary>
	Lost = 2,
	/// <summary>
	/// Fine for damaged books.
	/// </summary>
	Damaged = 3,
	/// <summary>
	///	Fine for other types of violations (e.g., policy breaches).
	///	</summary>
	Other = 4
}
