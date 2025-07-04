using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents a password reset token for secure password reset flows.
/// Supports UC005 (Reset Password).
/// Business Rule: BR-26 (password reset rate limiting).
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>
    /// The ID of the user requesting the password reset.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The secure token used for password reset verification.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The expiration date and time of the token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the token has been used (single-use enforcement).
    /// </summary>
    public bool IsUsed { get; set; }
    
    /// <summary>
    /// Navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = null!;
}