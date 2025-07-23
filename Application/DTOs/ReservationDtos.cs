using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// DTO for creating a new reservation - UC022
/// </summary>
public record CreateReservationRequest
{
    /// <summary>
    /// The ID of the member making the reservation.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The ID of the book being reserved.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Optional notes about the reservation.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// UI-Only: Used to remember if the form was loaded for a specific book.
    /// This tells the POST action how to render the view on failure.
    /// </summary>
    public bool IsSpecificBookMode { get; set; } = false;
}

/// <summary>
/// DTO for cancelling a reservation - UC023
/// </summary>
public record CancelReservationRequest
{
    /// <summary>
    /// The ID of the reservation to cancel.
    /// </summary>
    public int ReservationId { get; set; }

    /// <summary>
    /// Reason for cancellation (optional).
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Indicates if this is a staff-initiated cancellation.
    /// </summary>
    public bool IsStaffCancellation { get; set; } = false;
}

/// <summary>
/// DTO for fulfilling a reservation - UC024
/// </summary>
public record FulfillReservationRequest
{
    /// <summary>
    /// The ID of the reservation to fulfill.
    /// </summary>
    public int ReservationId { get; set; }

    /// <summary>
    /// The ID of the book copy being assigned to the reservation.
    /// </summary>
    public int BookCopyId { get; set; }

    /// <summary>
    /// The pickup deadline (optional, default is 72 hours from fulfillment).
    /// </summary>
    public DateTime? PickupDeadline { get; set; }

    /// <summary>
    /// Optional notes about the fulfillment.
    /// </summary>
    public string? FulfillmentNotes { get; set; }
}

/// <summary>
/// DTO for reservation search parameters - UC025
/// </summary>
public record ReservationSearchRequest : PagedRequest
{
    /// <summary>
    /// Optional member ID to filter reservations by member.
    /// </summary>
    public int? MemberId { get; set; }

    /// <summary>
    /// Optional book ID to filter reservations by book.
    /// </summary>
    public int? BookId { get; set; }

    /// <summary>
    /// Optional status to filter reservations by their status.
    /// </summary>
    public ReservationStatus? Status { get; set; }

    /// <summary>
    /// Optional start date for filtering reservations by date range.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Optional end date for filtering reservations by date range.
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Basic DTO for reservation information
/// </summary>
public record ReservationBasicDto
{
    /// <summary>
    /// The ID of the reservation.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ID of the member.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The User ID of the member (for notification targeting).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the book.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// The title of the book.
    /// </summary>
    public string BookTitle { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the assigned book copy, if applicable.
    /// </summary>
    public int? BookCopyId { get; set; }

    /// <summary>
    /// The date the reservation was made.
    /// </summary>
    public DateTime ReservationDate { get; set; }

    /// <summary>
    /// The status of the reservation.
    /// </summary>
    public ReservationStatus Status { get; set; }

    /// <summary>
    /// The queue position (for active reservations).
    /// </summary>
    public int? QueuePosition { get; set; }

    /// <summary>
    /// The pickup deadline date (for fulfilled reservations).
    /// </summary>
    public DateTime? PickupDeadline { get; set; }
}

/// <summary>
/// Detailed DTO for reservation information
/// </summary>
public class ReservationOverrideContext
{
    public bool IsOverride { get; set; }
    public string? Reason { get; set; }
    public List<string> OverriddenRules { get; set; } = new();
}

public record ReservationDetailDto : ReservationBasicDto
{
    /// <summary>
    /// The book's author.
    /// </summary>
    public string BookAuthor { get; set; } = string.Empty;

    /// <summary>
    /// The ISBN of the book.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Copy identifier (if fulfilled).
    /// </summary>
    public string? CopyNumber { get; set; }

    /// <summary>
    /// Estimated availability date (for active reservations).
    /// </summary>
    public DateTime? EstimatedAvailabilityDate { get; set; }

    /// <summary>
    /// Member contact email.
    /// </summary>
    public string MemberEmail { get; set; } = string.Empty;

    /// <summary>
    /// Member contact phone.
    /// </summary>
    public string? MemberPhone { get; set; }

    public ReservationOverrideContext? OverrideContext { get; set; }
}