using Domain.Enums;

namespace Domain.Entities;

public class Librarian : BaseEntity
{
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public LibrarianPrivileges Privileges { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}
