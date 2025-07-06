using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for authentication-related operations.
/// Supports UC002 (Login), UC004 (Change Password), UC005 (Reset Password), UC008 (Register Member).
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    
    Task<Result<int>> RegisterMemberAsync(RegisterRequest request);
    
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    
    Task<Result> RequestPasswordResetAsync(ResetPasswordRequest request);
    
    Task<Result> ConfirmPasswordResetAsync(ConfirmResetPasswordRequest request);
    
    Task<Result<DateTime?>> RecordFailedLoginAttemptAsync(string email);
    
    Task<Result<DateTime?>> CheckAccountLockoutStatusAsync(string email);
    
    Task<Result> ClearFailedLoginAttemptsAsync(int userId);
    
    Result ValidatePassword(string password);
}
