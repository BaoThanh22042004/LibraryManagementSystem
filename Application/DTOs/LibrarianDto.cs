using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for librarian information.
/// Librarian management is always tied to user management. All librarian creation, update, and deletion is handled through user commands/handlers (UC001, UC006, UC009).
/// </summary>
public class LibrarianDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// Data transfer object for creating a new librarian record.
/// Librarian creation is always performed as part of user creation (UC001).
/// </summary>
public class CreateLibrarianDto
{
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}

/// <summary>
/// Data transfer object for updating an existing librarian record.
/// Librarian update is always performed as part of user update (UC006).
/// </summary>
public class UpdateLibrarianDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
}

/// <summary>
/// Extended librarian information for detailed views.
/// Librarian management is always tied to user management. All librarian creation, update, and deletion is handled through user commands/handlers.
/// </summary>
public class LibrarianDetailsDto : LibrarianDto
{
    // Additional properties specific to librarian details view
    public bool IsActive => User != null;
}

/// <summary>
/// Data transfer object for librarian self-signup/registration.
/// Librarian registration is always performed as part of user registration (UC001).
/// </summary>
public class LibrarianSignUpDto
{
    // User information
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    
    // Librarian information
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
}