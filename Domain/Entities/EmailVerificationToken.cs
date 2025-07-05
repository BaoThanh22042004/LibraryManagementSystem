using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Represents an email verification token for secure email verification flows.
/// Supports UC003 Exception 3.0.E3 (Email Verification Required).
/// </summary>
public class EmailVerificationToken : BaseEntity
{
    /// <summary>
    /// The ID of the user requesting the email verification.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The secure token used for email verification.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The new email address that needs to be verified.
    /// </summary>
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>
    /// The old email address that was changed from.
    /// </summary>
    public string OldEmail { get; set; } = string.Empty;

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