using Application.Features.Reservations.Queries;
using FluentValidation;

namespace Application.Features.Reservations.Validators
{
    public class GetPaginatedReservationsQueryValidator : AbstractValidator<GetPaginatedReservationsQuery>
    {
        public GetPaginatedReservationsQueryValidator()
        {
            RuleFor(x => x.PagedRequest.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PagedRequest.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size must not exceed 100 items");
                
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.SearchTerm))
                .WithMessage("Search term must not exceed 100 characters");
        }
    }
}