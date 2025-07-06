using Application.Common;

namespace Application.Interfaces;

/// <summary>
/// Service interface for email operations.
/// Supports UC003 (Update Profile), UC005 (Reset Password).
/// </summary>
public interface IEmailService
{
    Task<Result> SendPasswordResetEmailAsync(string email, string fullName, string resetToken);

    Task<Result> SendEmailVerificationAsync(string email, string fullName, string verificationToken);

    Task<Result> SendEmailAsync(string email, string subject, string message);
}
