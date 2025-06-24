using Domain.Enums;

namespace Application.DTOs;

public class ReservationDto
{
    public int Id { get; set; }
    public DateTime ReservationDate { get; set; }
    public ReservationStatus Status { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int? BookCopyId { get; set; }
}

public class CreateReservationDto
{
    public int MemberId { get; set; }
    public int BookId { get; set; }
}

public class UpdateReservationDto
{
    public ReservationStatus Status { get; set; }
    public int? BookCopyId { get; set; }
}