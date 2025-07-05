using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Request DTO for user login (UC002 - Login).
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
}

/// <summary>
/// Response DTO for successful login (UC002 - Login).
/// </summary>
public class LoginResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool RequirePasswordChange { get; set; }
}

/// <summary>
/// Request DTO for password reset request (UC005 - Reset Password).
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for confirming password reset (UC005 - Reset Password).
/// </summary>
public class ConfirmResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for changing password (UC004 - Change Password).
/// </summary>
public class ChangePasswordRequest
{
    public int UserId { get; set; }
    
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for user registration (UC008 - Register Member).
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
    
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; set; }
    
    [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
    public string? Address { get; set; }
    
    [MaxLength(20, ErrorMessage = "Membership number cannot exceed 20 characters")]
    public string? PreferredMembershipNumber { get; set; }
}
