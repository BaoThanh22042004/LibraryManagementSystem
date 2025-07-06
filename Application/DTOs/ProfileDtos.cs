namespace Application.DTOs;

/// <summary>
/// Request DTO for updating user profile (UC003 - Update Profile).
/// </summary>
public record UpdateProfileRequest
{
    public int UserId { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    public string? Address { get; set; }
}

/// <summary>
/// Response DTO for profile information (UC003 - Update Profile).
/// </summary>
public record ProfileDto
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
public record VerifyEmailChangeRequest
{
    public string Token { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
}
