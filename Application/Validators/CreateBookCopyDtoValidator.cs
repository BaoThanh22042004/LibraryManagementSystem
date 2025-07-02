using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentValidation;

namespace Application.Validators;

public class CreateBookCopyDtoValidator : AbstractValidator<CreateBookCopyDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookCopyDtoValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required.")
            .MustAsync(BookExistsAsync).WithMessage("Book with the specified ID does not exist.");

        RuleFor(x => x.CopyNumber)
            .MaximumLength(50).WithMessage("Copy number cannot be longer than 50 characters.")
            .MustAsync(BeUniqueCopyNumberAsync).WithMessage("Copy number already exists for this book.")
            .When(x => !string.IsNullOrWhiteSpace(x.CopyNumber));
    }

    private async Task<bool> BookExistsAsync(int bookId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<Book>().ExistsAsync(b => b.Id == bookId);
    }

    private async Task<bool> BeUniqueCopyNumberAsync(CreateBookCopyDto dto, string copyNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(copyNumber))
            return true;

        return !await _unitOfWork.Repository<BookCopy>().ExistsAsync(
            bc => bc.CopyNumber == copyNumber && bc.BookId == dto.BookId);
    }
}