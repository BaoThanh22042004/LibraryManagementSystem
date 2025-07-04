using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a fine imposed on a library member for overdue returns, damages, or other reasons.
/// Supports UC026–UC029 (fine calculation, payment, waiver, and history).
/// Business Rule: BR-15 (overdue fine calculation).
/// </summary>
public class Fine : BaseEntity
{
	/// <summary>
	/// The ID of the member who received the fine.
	/// </summary>
	public int MemberId { get; set; }

	/// <summary>
	/// The ID of the related loan, if applicable (nullable for manual fines).
	/// </summary>
	public int? LoanId { get; set; }

	/// <summary>
	/// The type of fine (e.g., overdue, damage).
	/// </summary>
	public FineType Type { get; set; }

	/// <summary>
	/// The monetary amount of the fine.
	/// </summary>
	public decimal Amount { get; set; }

	/// <summary>
	/// The date the fine was imposed.
	/// </summary>
	public DateTime FineDate { get; set; }

	/// <summary>
	/// The current status of the fine (pending, paid, waived).
	/// </summary>
	public FineStatus Status { get; set; }

	/// <summary>
	/// Optional description or reason for the fine.
	/// </summary>
	[MaxLength(500)]
	public string Description { get; set; } = string.Empty;

	// Navigation properties
	/// <summary>
	/// Navigation property to the fined member.
	/// </summary>
	public Member Member { get; set; } = null!;
	/// <summary>
	/// Navigation property to the related loan, if applicable.
	/// </summary>
	public Loan? Loan { get; set; }
}
