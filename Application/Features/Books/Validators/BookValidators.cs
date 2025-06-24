using Application.Features.Books.Commands;
using FluentValidation;

namespace Application.Features.Books.Validators;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.BookDto.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters");

        RuleFor(x => x.BookDto.Author)
            .NotEmpty().WithMessage("Author is required")
            .MaximumLength(255).WithMessage("Author cannot exceed 255 characters");

        RuleFor(x => x.BookDto.ISBN)
            .NotEmpty().WithMessage("ISBN is required")
            .MaximumLength(20).WithMessage("ISBN cannot exceed 20 characters")
            .Matches(@"^[0-9-]+$").WithMessage("ISBN can only contain numbers and hyphens");

        RuleFor(x => x.BookDto.Publisher)
            .MaximumLength(255).WithMessage("Publisher cannot exceed 255 characters");

        RuleFor(x => x.BookDto.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters");

        RuleFor(x => x.BookDto.CoverImageUrl)
            .MaximumLength(255).WithMessage("Cover image URL cannot exceed 255 characters");

        RuleFor(x => x.BookDto.InitialCopiesCount)
            .GreaterThanOrEqualTo(0).WithMessage("Initial copies count must be greater than or equal to 0");
    }
}

public class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Book ID must be greater than 0");

        RuleFor(x => x.BookDto.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters");

        RuleFor(x => x.BookDto.Author)
            .NotEmpty().WithMessage("Author is required")
            .MaximumLength(255).WithMessage("Author cannot exceed 255 characters");

        RuleFor(x => x.BookDto.Publisher)
            .MaximumLength(255).WithMessage("Publisher cannot exceed 255 characters");

        RuleFor(x => x.BookDto.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters");

        RuleFor(x => x.BookDto.CoverImageUrl)
            .MaximumLength(255).WithMessage("Cover image URL cannot exceed 255 characters");
    }
}

public class DeleteBookCommandValidator : AbstractValidator<DeleteBookCommand>
{
    public DeleteBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Book ID must be greater than 0");
    }
}