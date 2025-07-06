using Application.DTOs;
using FluentValidation;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Validators;

/// <summary>
/// Validator for CreateReservationDto - UC022 (Reserve Book)
/// Enforces business rules for reservation creation
/// </summary>
public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).WithMessage("Invalid member ID");

        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Invalid book ID");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for CancelReservationDto - UC023 (Cancel Reservation)
/// </summary>
public class CancelReservationRequestValidator : AbstractValidator<CancelReservationRequest>
{
    public CancelReservationRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .GreaterThan(0).WithMessage("Invalid reservation ID");

        When(x => x.IsStaffCancellation, () => {
            RuleFor(x => x.CancellationReason)
                .NotEmpty().WithMessage("Cancellation reason is required for staff-initiated cancellations")
                .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters");
        });

        When(x => !x.IsStaffCancellation, () => {
            RuleFor(x => x.CancellationReason)
                .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters");
        });
    }
}

/// <summary>
/// Validator for FulfillReservationDto - UC024 (Fulfill Reservation)
/// </summary>
public class FulfillReservationRequestValidator : AbstractValidator<FulfillReservationRequest>
{
    public FulfillReservationRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .GreaterThan(0).WithMessage("Invalid reservation ID");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("Invalid book copy ID");

        When(x => x.PickupDeadline.HasValue, () => {
            RuleFor(x => x.PickupDeadline)
                .Must(date => date > DateTime.UtcNow).WithMessage("Pickup deadline must be in the future")
                .Must(date => date <= DateTime.UtcNow.AddDays(7)).WithMessage("Pickup deadline cannot exceed 7 days from now");
        });

        RuleFor(x => x.FulfillmentNotes)
            .MaximumLength(500).WithMessage("Fulfillment notes cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for ReservationSearchParametersDto - UC025 (View Reservations)
/// </summary>
public class ReservationSearchRequestValidator : AbstractValidator<ReservationSearchRequest>
{
    public ReservationSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");

        When(x => x.MemberId.HasValue, () => {
            RuleFor(x => x.MemberId)
                .GreaterThan(0).WithMessage("Member ID must be greater than 0");
        });

        When(x => x.BookId.HasValue, () => {
            RuleFor(x => x.BookId)
                .GreaterThan(0).WithMessage("Book ID must be greater than 0");
        });

        When(x => x.Status.HasValue, () => {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid reservation status");
        });

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () => {
            RuleFor(x => x)
                .Must(x => x.FromDate <= x.ToDate)
                .WithMessage("From date must be earlier than or equal to To date");
        });
    }
}