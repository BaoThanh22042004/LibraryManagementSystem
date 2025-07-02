using Application.Features.Reservations.Queries;
using FluentValidation;

namespace Application.Features.Reservations.Validators
{
    public class GetNextActiveReservationQueryValidator : AbstractValidator<GetNextActiveReservationQuery>
    {
        public GetNextActiveReservationQueryValidator()
        {
            RuleFor(x => x.BookId)
                .GreaterThan(0)
                .WithMessage("Valid Book ID is required");
        }
    }
    
    public class GetReservationByIdQueryValidator : AbstractValidator<GetReservationByIdQuery>
    {
        public GetReservationByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Valid Reservation ID is required");
        }
    }
    
    public class GetReservationsByBookIdQueryValidator : AbstractValidator<GetReservationsByBookIdQuery>
    {
        public GetReservationsByBookIdQueryValidator()
        {
            RuleFor(x => x.BookId)
                .GreaterThan(0)
                .WithMessage("Valid Book ID is required");
        }
    }
    
    public class GetReservationsByMemberIdQueryValidator : AbstractValidator<GetReservationsByMemberIdQuery>
    {
        public GetReservationsByMemberIdQueryValidator()
        {
            RuleFor(x => x.MemberId)
                .GreaterThan(0)
                .WithMessage("Valid Member ID is required");
        }
    }
}