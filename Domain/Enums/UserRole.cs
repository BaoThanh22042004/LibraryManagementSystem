namespace Domain.Enums;

/// <summary>
/// Represents the role of a user in the system (RBAC).
/// Used in user management and access control (UC001–UC009).
/// Business Rules: BR-02 (user roles), BR-24 (RBAC).
/// </summary>
public enum UserRole
{
	/// <summary>
	/// Regular library member (patron).
	/// </summary>
	Member = 1,
	/// <summary>
	/// Librarian (staff member with management permissions).
	/// </summary>
	Librarian = 2,
	/// <summary>
	/// System administrator (full permissions).
	/// </summary>
	Admin = 3
}
