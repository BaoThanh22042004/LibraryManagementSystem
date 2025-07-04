using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for user information.
/// Used in UC001–UC009 (user management, authentication, profile, etc.).
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Data transfer object for creating a new user account.
/// Used in UC001 (Create User).
/// </summary>
public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Data transfer object for updating an existing user account.
/// Used in UC006 (Update User Info).
/// </summary>
public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Data transfer object for changing a user's password.
/// Used in UC004 (Change Password).
/// </summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for user login.
/// Used in UC002 (Login).
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

/// <summary>
/// Data transfer object for login response, including user info and token.
/// Used in UC002 (Login).
/// </summary>
public class LoginResponseDto
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for requesting a password reset.
/// Used in UC005 (Reset Password).
/// </summary>
public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for resetting a user's password.
/// Used in UC005 (Reset Password).
/// </summary>
public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating a user's own profile.
/// Used in UC003 (Update Profile).
/// </summary>
public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Data transfer object for admin or staff updating a user account.
/// Used in UC006 (Update User Info) by staff/admin.
/// </summary>
public class AdminUpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? StatusChangeReason { get; set; }
}

/// <summary>
/// Extended user information for detailed views, including member/librarian info.
/// Used in UC007 (View User Info).
/// </summary>
public class UserDetailsDto : UserDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public MemberDto? Member { get; set; }
    public LibrarianDto? Librarian { get; set; }
}

/// <summary>
/// Data transfer object for member self-registration.
/// Used in UC008 (Register Member).
/// </summary>
public class RegisterMemberDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PreferredMembershipNumber { get; set; }
}