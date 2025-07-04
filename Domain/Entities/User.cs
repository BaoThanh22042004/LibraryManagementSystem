using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a user account in the library system.
/// Supports UC001–UC009 (user management, authentication, profile, password, etc.).
/// Business Rules: BR-01 (role permissions), BR-02 (roles), BR-03 (info access), BR-04 (privacy), BR-05 (password security), BR-24 (RBAC).
/// </summary>
public class User : BaseEntity
{
	/// <summary>
	/// The full name of the user.
	/// </summary>
	[MaxLength(100)]
	public string FullName { get; set; } = string.Empty;

	/// <summary>
	/// The unique email address of the user (used for login and notifications).
	/// </summary>
	[MaxLength(100)]
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// The hashed password for secure authentication.
	/// </summary>
	public string PasswordHash { get; set; } = string.Empty;

	/// <summary>
	/// The role of the user (Member, Librarian, Admin).
	/// </summary>
	public UserRole Role { get; set; }

	/// <summary>
	/// The current status of the user account (Active, Suspended, etc.).
	/// </summary>
	public UserStatus Status { get; set; } = UserStatus.Active;

	/// <summary>
	/// Optional phone number for the user.
	/// </summary>
	[MaxLength(20)]
	public string? Phone { get; set; }

	/// <summary>
	/// Optional address for the user.
	/// </summary>
	[MaxLength(255)]
	public string? Address { get; set; }

	// Navigation properties
	/// <summary>
	/// Navigation property to the member profile, if applicable.
	/// </summary>
	public Member? Member { get; set; }
	/// <summary>
	/// Navigation property to the librarian profile, if applicable.
	/// </summary>
	public Librarian? Librarian { get; set; }
	/// <summary>
	/// Collection of notifications sent to this user.
	/// </summary>
	public ICollection<Notification> Notifications { get; set; } = [];
}
