using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Request DTO for updating user profile (UC003 - Update Profile).
/// </summary>
public class UpdateProfileRequest
{
    public int UserId { get; set; }
    
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; set; }
    
    [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
    public string? Address { get; set; }
}

/// <summary>
/// Response DTO for profile information (UC003 - Update Profile).
/// </summary>
public class ProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PendingEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Request DTO for verifying email change (UC003 - Update Profile).
/// </summary>
public class VerifyEmailChangeRequest
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
