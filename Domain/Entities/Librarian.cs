using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Librarian : BaseEntity
{
    public int UserId { get; set; }
    [MaxLength(20)]
	public string EmployeeId { get; set; } = string.Empty;
    [MaxLength(100)]
	public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public LibrarianPrivileges Privileges { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}
