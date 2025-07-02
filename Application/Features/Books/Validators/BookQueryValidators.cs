using Application.Features.Books.Queries;
using FluentValidation;

namespace Application.Features.Books.Validators;

public class GetBooksByCategoryQueryValidator : AbstractValidator<GetBooksByCategoryQuery>
{
    public GetBooksByCategoryQueryValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category ID must be greater than 0");
            
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");
            
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");
    }
}

public class SearchBooksQueryValidator : AbstractValidator<SearchBooksQuery>
{
    public SearchBooksQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");
            
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");
            
        When(x => x.CategoryId.HasValue, () => {
            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Category ID must be greater than 0");
        });
        
        // At least one search parameter should be provided
        RuleFor(x => new { x.SearchTerm, x.Title, x.Author, x.ISBN, x.CategoryId })
            .Must(x => !string.IsNullOrWhiteSpace(x.SearchTerm) || 
                       !string.IsNullOrWhiteSpace(x.Title) || 
                       !string.IsNullOrWhiteSpace(x.Author) || 
                       !string.IsNullOrWhiteSpace(x.ISBN) || 
                       x.CategoryId.HasValue)
            .When(x => string.IsNullOrWhiteSpace(x.SearchTerm) && 
                       string.IsNullOrWhiteSpace(x.Title) && 
                       string.IsNullOrWhiteSpace(x.Author) && 
                       string.IsNullOrWhiteSpace(x.ISBN) && 
                       !x.CategoryId.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("At least one search parameter must be provided");
    }
}