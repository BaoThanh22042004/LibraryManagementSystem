using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for authentication-related operations.
/// Supports UC002 (Login), UC004 (Change Password), UC005 (Reset Password), UC008 (Register Member).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user based on email and password.
    /// Supports UC002 (Login).
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Result with user login information if successful</returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Registers a new member with the system.
    /// Supports UC008 (Register Member).
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Result with user ID if successful</returns>
    Task<Result<int>> RegisterMemberAsync(RegisterRequest request);
    
    /// <summary>
    /// Changes the password for an authenticated user.
    /// Supports UC004 (Change Password).
    /// </summary>
    /// <param name="request">Password change information</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    
    /// <summary>
    /// Initiates a password reset process by sending a reset token to the user's email.
    /// Supports UC005 (Reset Password).
    /// </summary>
    /// <param name="request">Password reset request information</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> RequestPasswordResetAsync(ResetPasswordRequest request);
    
    /// <summary>
    /// Confirms a password reset using a valid token.
    /// Supports UC005 (Reset Password).
    /// </summary>
    /// <param name="request">Password reset confirmation information</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ConfirmPasswordResetAsync(ConfirmResetPasswordRequest request);
    
    /// <summary>
    /// Records a failed login attempt for a user and manages account lockout.
    /// Supports UC002 Alternative Flow 2.3 (Account Security Lockout).
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Result with lockout information</returns>
    Task<Result<DateTime?>> RecordFailedLoginAttemptAsync(string email);
    
    /// <summary>
    /// Checks if an account is currently locked due to failed login attempts.
    /// Supports UC002 Alternative Flow 2.3 (Account Security Lockout).
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Result with lockout status and end time</returns>
    Task<Result<DateTime?>> CheckAccountLockoutStatusAsync(string email);
    
    /// <summary>
    /// Clears failed login attempts for a user after successful authentication.
    /// Supports UC002 (Login).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Task completion</returns>
    Task ClearFailedLoginAttemptsAsync(int userId);
    
    /// <summary>
    /// Validates a password against security requirements.
    /// Supports UC001, UC004, UC005, UC008.
    /// Business Rule: BR-05 (Password Security).
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Result with validation result</returns>
    Result ValidatePassword(string password);
}
