using Application.DTOs;
using FluentValidation;
using Domain.Enums;

namespace Application.Validators;

/// <summary>
/// Validator for CreateBookDto
/// Enforces business rules for book creation
/// </summary>
public class CreateBookDtoValidator : AbstractValidator<CreateBookDto>
{
    public CreateBookDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Book title is required");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author name is required");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required")
            .MaximumLength(20).WithMessage("ISBN cannot exceed 20 characters")
            .Matches(@"^[0-9\-]+$").WithMessage("ISBN should contain only numbers and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.InitialCopies)
            .GreaterThanOrEqualTo(1).WithMessage("At least one copy must be created");

        RuleFor(x => x.CategoryIds)
            .Must(categories => categories.Count > 0)
            .WithMessage("At least one category must be selected");
    }
}

/// <summary>
/// Validator for UpdateBookDto
/// Enforces business rules for book updates
/// </summary>
public class UpdateBookDtoValidator : AbstractValidator<UpdateBookDto>
{
    public UpdateBookDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid book ID");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Book title is required");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author name is required");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required")
            .MaximumLength(20).WithMessage("ISBN cannot exceed 20 characters")
            .Matches(@"^[0-9\-]+$").WithMessage("ISBN should contain only numbers and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid book status");

        RuleFor(x => x.CategoryIds)
            .Must(categories => categories.Count > 0)
            .WithMessage("At least one category must be selected");
    }
}

/// <summary>
/// Validator for BookSearchParametersDto
/// Enforces valid search parameters
/// </summary>
public class BookSearchParametersDtoValidator : AbstractValidator<BookSearchParametersDto>
{
    public BookSearchParametersDtoValidator()
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
    }
}