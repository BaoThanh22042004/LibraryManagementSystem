using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a library member (patron) in the system.
/// Supports UC001 (Create User), UC008 (Register Member), UC006 (Update User Info), and related flows.
/// Business Rules: BR-01 (user role permissions), BR-02 (user roles), BR-16 (loan eligibility), BR-18 (reservation limit), BR-23 (deletion restriction).
/// </summary>
public class Member : BaseEntity
{
	/// <summary>
	/// The ID of the associated user account.
	/// </summary>
	public int UserId { get; set; }

	/// <summary>
	/// The unique membership number for the member.
	/// </summary>
	[MaxLength(20)]
	public string MembershipNumber { get; set; } = string.Empty;

	/// <summary>
	/// The date the membership started.
	/// </summary>
	public DateTime MembershipStartDate { get; set; }

	/// <summary>
	/// The current status of the membership (Active, Suspended, Expired, etc.).
	/// </summary>
	public MembershipStatus MembershipStatus { get; set; }

	/// <summary>
	/// The total outstanding fines for the member (used for eligibility checks).
	/// </summary>
	public decimal OutstandingFines { get; set; }

	// Navigation properties
	/// <summary>
	/// Navigation property to the associated user account.
	/// </summary>
	public User User { get; set; } = null!;

	/// <summary>
	/// Collection of loans associated with this member.
	/// </summary>
	public ICollection<Loan> Loans { get; set; } = [];

	/// <summary>
	/// Collection of reservations made by this member.
	/// </summary>
	public ICollection<Reservation> Reservations { get; set; } = [];

	/// <summary>
	/// Collection of fines associated with this member.
	/// </summary>
	public ICollection<Fine> Fines { get; set; } = [];
}
