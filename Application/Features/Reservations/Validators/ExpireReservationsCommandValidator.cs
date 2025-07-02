using Application.Features.Reservations.Commands;
using FluentValidation;

namespace Application.Features.Reservations.Validators
{
    public class ExpireReservationsCommandValidator : AbstractValidator<ExpireReservationsCommand>
    {
        public ExpireReservationsCommandValidator()
        {
            // No validation needed for this command as it doesn't have any parameters
        }
    }
}