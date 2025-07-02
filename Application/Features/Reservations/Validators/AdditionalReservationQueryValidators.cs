using Application.Features.Reservations.Queries;
using FluentValidation;

namespace Application.Features.Reservations.Validators
{
    public class HasReservationsForBookQueryValidator : AbstractValidator<HasReservationsForBookQuery>
    {
        public HasReservationsForBookQueryValidator()
        {
            RuleFor(x => x.BookId)
                .GreaterThan(0)
                .WithMessage("Valid Book ID is required");
        }
    }
    
    public class GetReservationCountForMemberQueryValidator : AbstractValidator<GetReservationCountForMemberQuery>
    {
        public GetReservationCountForMemberQueryValidator()
        {
            RuleFor(x => x.MemberId)
                .GreaterThan(0)
                .WithMessage("Valid Member ID is required");
        }
    }
}