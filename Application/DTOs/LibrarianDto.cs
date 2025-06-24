using Domain.Enums;

namespace Application.DTOs;

public class LibrarianDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public UserDto User { get; set; } = null!;
}

public class CreateLibrarianDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.Now;
    public int UserId { get; set; }
}

public class UpdateLibrarianDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
}