using Application.Common;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Request DTO for creating a user (UC001 - Create User).
/// </summary>
public class CreateUserRequest
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
    
    [Required(ErrorMessage = "User role is required")]
    public UserRole Role { get; set; }
    
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; set; }
    
    [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
    public string? Address { get; set; }
    
    // For Member role
    [MaxLength(20, ErrorMessage = "Membership number cannot exceed 20 characters")]
    public string? MembershipNumber { get; set; }
    
    // For Librarian role
    [MaxLength(20, ErrorMessage = "Employee ID cannot exceed 20 characters")]
    public string? EmployeeId { get; set; }
}

/// <summary>
/// Response DTO for user details (UC007 - View User Info).
/// </summary>
public class UserDetailsResponse
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
    public MemberDetailsResponse? MemberDetails { get; set; }
    
    // For Librarian role
    public LibrarianDetailsResponse? LibrarianDetails { get; set; }
}

/// <summary>
/// Request DTO for updating a user (UC006 - Update User Info).
/// </summary>
public class UpdateUserRequest
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? Phone { get; set; }
    
    [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
    public string? Address { get; set; }
    
    public UserStatus? Status { get; set; }
    
    // For staff users only
    public string? StatusChangeReason { get; set; }
}

/// <summary>
/// Response DTO for member details (UC007 - View User Info).
/// </summary>
public class MemberDetailsResponse
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
public class LibrarianDetailsResponse
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}

/// <summary>
/// Request DTO for user search/pagination (UC007 - View User Info).
/// </summary>
public class UserSearchRequest
{
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Response DTO for user search results with pagination (UC007 - View User Info).
/// </summary>
public class UserSearchResponse : PagedResult<UserSummaryDto> { }

/// <summary>
/// DTO for summary user information in lists (UC007 - View User Info).
/// </summary>
public class UserSummaryDto
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
