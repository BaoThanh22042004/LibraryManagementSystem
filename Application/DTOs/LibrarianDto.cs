using Domain.Enums;

namespace Application.DTOs;

public class LibrarianDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public UserDto? User { get; set; }
}

public class CreateLibrarianDto
{
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}

public class UpdateLibrarianDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
}

public class LibrarianDetailsDto : LibrarianDto
{
    // Additional properties specific to librarian details view
    public bool IsActive => User != null;
}

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