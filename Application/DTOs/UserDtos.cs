using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request DTO for creating a user (UC001 - Create User).
/// </summary>
public record CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public UserRole Role { get; set; }
    
    public string? Phone { get; set; }
    
    public string? Address { get; set; }
    
    // For Member role
    public string? MembershipNumber { get; set; }
    
    // For Librarian role
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Response DTO for user details (UC007 - View User Info).
/// </summary>
public record UserDetailsDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // For Member role
    public MemberDetailsDto? MemberDetails { get; set; }
    
    // For Librarian role
    public LibrarianDetailsDto? LibrarianDetails { get; set; }
}

/// <summary>
/// Request DTO for updating a user (UC006 - Update User Info).
/// </summary>
public record UpdateUserRequest
{
    public int Id { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    public string? Address { get; set; }
    
    public UserStatus? Status { get; set; }
    
    // For staff users only
    public string? StatusChangeReason { get; set; }
}

/// <summary>
/// Response DTO for member details (UC007 - View User Info).
/// </summary>
public record MemberDetailsDto
{
    public int Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public DateTime MembershipStartDate { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public decimal OutstandingFines { get; set; }
    public int ActiveLoans { get; set; }
    public int ActiveReservations { get; set; }
}

/// <summary>
/// Response DTO for librarian details (UC007 - View User Info).
/// </summary>
public record LibrarianDetailsDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}

/// <summary>
/// Request DTO for user search/pagination (UC007 - View User Info).
/// </summary>
public record UserSearchRequest : PagedRequest
{
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
}

/// <summary>
/// DTO for summary user information in lists (UC007 - View User Info).
/// </summary>
public record UserBasicDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // For Member role
    public string? MembershipNumber { get; set; }
    public MembershipStatus? MembershipStatus { get; set; }
    
    // For Librarian role
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Validation results for member deletion checks.
/// </summary>
public class MemberDeletionValidationDto
{
    public bool CanDelete { get; set; }
    public bool HasActiveLoans { get; set; }
    public bool HasActiveReservations { get; set; }
    public bool HasUnpaidFines { get; set; }
    public string Message { get; set; } = string.Empty;
}