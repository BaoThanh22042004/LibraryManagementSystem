using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request DTO for user login (UC002 - Login).
/// </summary>
public record LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

/// <summary>
/// Response DTO for successful login (UC002 - Login).
/// </summary>
public record LoginResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

/// <summary>
/// Request DTO for password reset request (UC005 - Reset Password).
/// </summary>
public record ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for confirming password reset (UC005 - Reset Password).
/// </summary>
public record ConfirmResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string NewPassword { get; set; } = string.Empty;
    
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for changing password (UC004 - Change Password).
/// </summary>
public record ChangePasswordRequest
{
    public int UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for user registration (UC008 - Register Member).
/// </summary>
public record RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PreferredMembershipNumber { get; set; }
}
