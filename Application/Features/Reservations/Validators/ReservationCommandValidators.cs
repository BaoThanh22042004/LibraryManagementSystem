using Application.Features.Reservations.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Reservations.Validators;

public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required");
        RuleFor(x => x.ReservationDto.Status)
            .IsInEnum().WithMessage("Invalid reservation status");
        When(x => x.ReservationDto.BookCopyId.HasValue, () => {
            RuleFor(x => x.ReservationDto.BookCopyId!.Value)
                .GreaterThan(0).WithMessage("Book copy ID must be greater than 0");
        });
    }
}

public class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required");
    }
}

public class FulfillReservationCommandValidator : AbstractValidator<FulfillReservationCommand>
{
    public FulfillReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required");
        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("Book copy ID is required");
    }
}

public class DeleteReservationCommandValidator : AbstractValidator<DeleteReservationCommand>
{
    public DeleteReservationCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required");
    }
}