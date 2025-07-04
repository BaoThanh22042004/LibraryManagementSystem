using Application.Features.Reservations.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Reservations.Validators;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.ReservationDto.MemberId)
            .GreaterThan(0).WithMessage("Member ID is required");
        RuleFor(x => x.ReservationDto.BookId)
            .GreaterThan(0).WithMessage("Book ID is required");
    }
}