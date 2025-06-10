using Domain.Enums;

namespace Domain.Entities;

public class Reservation : BaseEntity
{
    public int MemberId { get; set; }
    public int BookId { get; set; }
    public DateTime ReservationDate { get; set; }
    public ReservationStatus Status { get; set; }
    public int QueuePosition { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
