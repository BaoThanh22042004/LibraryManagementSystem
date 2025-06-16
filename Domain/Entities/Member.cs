using Domain.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Member : BaseEntity
{
    public int UserId { get; set; }
    [MaxLength(20)]
	public string MembershipNumber { get; set; } = string.Empty;
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    [DefaultValue(0)]
	public decimal OutstandingFines { get; set; }
    [DefaultValue(0)]
	public int CurrentLoanCount { get; set; }
    [DefaultValue(0)]
	public int CurrentReservationCount { get; set; }
    public bool NotificationPreference { get; set; } = true;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Fine> Fines { get; set; } = [];
}
