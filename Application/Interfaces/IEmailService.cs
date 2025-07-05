using Application.Common;

namespace Application.Interfaces;

/// <summary>
/// Service interface for email operations.
/// Supports UC003 (Update Profile), UC005 (Reset Password).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email with a reset token.
    /// Supports UC005 (Reset Password).
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="fullName">Recipient's full name</param>
    /// <param name="resetToken">Password reset token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendPasswordResetEmailAsync(string email, string fullName, string resetToken);
    /// <summary>
    /// Sends an email verification email with a verification token.
    /// Supports UC003 (Update Profile), Exception 3.0.E3 (Email Verification Required).
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="fullName">Recipient's full name</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendEmailVerificationAsync(string email, string fullName, string verificationToken);
    /// <summary>
    /// Sends a generic email.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email body/message</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendEmailAsync(string email, string subject, string message);
}
