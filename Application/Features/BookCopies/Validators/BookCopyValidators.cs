using Application.Features.BookCopies.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.BookCopies.Validators;

public class CreateBookCopyCommandValidator : AbstractValidator<CreateBookCopyCommand>
{
    public CreateBookCopyCommandValidator()
    {
        RuleFor(x => x.BookCopyDto.BookId)
            .GreaterThan(0).WithMessage("Book ID must be greater than 0");

        RuleFor(x => x.BookCopyDto.CopyNumber)
            .MaximumLength(50).WithMessage("Copy number cannot exceed 50 characters");
    }
}



public class UpdateBookCopyStatusCommandValidator : AbstractValidator<UpdateBookCopyStatusCommand>
{
    public UpdateBookCopyStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Book Copy ID must be greater than 0");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid book copy status");
    }
}

public class DeleteBookCopyCommandValidator : AbstractValidator<DeleteBookCopyCommand>
{
    public DeleteBookCopyCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Book Copy ID must be greater than 0");
    }
}