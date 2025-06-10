using Domain.Enums;

namespace Domain.Entities;

public class Member : BaseEntity
{
    public int UserId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public decimal OutstandingFines { get; set; }
    public int CurrentLoanCount { get; set; }
    public int CurrentReservationCount { get; set; }
    public bool NotificationPreference { get; set; } = true;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
