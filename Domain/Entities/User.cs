using Domain.Enums;

namespace Domain.Entities;

public class User : SoftDeleteEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? AccountLockedUntil { get; set; }
    public bool IsEmailVerified { get; set; }
    
    // Navigation properties
    public Member? Member { get; set; }
    public Librarian? Librarian { get; set; }
}
