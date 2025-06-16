using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class User : SoftDeleteEntity
{
    [MaxLength(100)]
	public string FullName { get; set; } = string.Empty;
    [MaxLength(100)]
	public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(20)]
	public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsEmailVerified { get; set; }
    
    // Navigation properties
    public Member? Member { get; set; }
    public Librarian? Librarian { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
}
