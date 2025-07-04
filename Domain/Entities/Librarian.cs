using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a librarian (staff member) in the library system.
/// Supports UC001 (Create User), UC006 (Update User Info), UC009 (Delete User).
/// Business Rules: BR-01 (user role permissions), BR-02 (user roles).
/// </summary>
public class Librarian : BaseEntity
{
	/// <summary>
	/// The ID of the associated user account.
	/// </summary>
	public int UserId { get; set; }

	/// <summary>
	/// The unique employee identifier for the librarian.
	/// </summary>
	[MaxLength(20)]
	public string EmployeeId { get; set; } = string.Empty;

	/// <summary>
	/// The date the librarian was hired.
	/// </summary>
	public DateTime HireDate { get; set; }

	/// <summary>
	/// Navigation property to the associated user account.
	/// </summary>
	public User User { get; set; } = null!;
}
