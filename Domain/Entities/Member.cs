using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Member : BaseEntity
{
	public int UserId { get; set; }
	[MaxLength(20)]
	public string MembershipNumber { get; set; } = string.Empty;
	public DateTime MembershipStartDate { get; set; }
	public MembershipStatus MembershipStatus { get; set; }
	public decimal OutstandingFines { get; set; }

	// Navigation properties
	public User User { get; set; } = null!;
	public ICollection<Loan> Loans { get; set; } = [];
	public ICollection<Reservation> Reservations { get; set; } = [];
	public ICollection<Fine> Fines { get; set; } = [];
}
