using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class User : BaseEntity
{
	[MaxLength(100)]
	public string FullName { get; set; } = string.Empty;
	[MaxLength(100)]
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public UserRole Role { get; set; }

	// Navigation properties
	public Member? Member { get; set; }
	public Librarian? Librarian { get; set; }
	public ICollection<Notification> Notifications { get; set; } = [];
}
