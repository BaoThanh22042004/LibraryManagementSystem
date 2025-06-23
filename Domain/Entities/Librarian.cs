using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Librarian : BaseEntity
{
	public int UserId { get; set; }
	[MaxLength(20)]
	public string EmployeeId { get; set; } = string.Empty;
	public DateTime HireDate { get; set; }

	// Navigation properties
	public User User { get; set; } = null!;
}
