using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentValidation;

namespace Application.Validators;

public class UpdateBookCopyDtoValidator : AbstractValidator<UpdateBookCopyDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private int _bookCopyId;
    private int _bookId;

    public UpdateBookCopyDtoValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.CopyNumber)
            .NotEmpty().WithMessage("Copy number is required.")
            .MaximumLength(50).WithMessage("Copy number cannot be longer than 50 characters.")
            .MustAsync(BeUniqueCopyNumberAsync).WithMessage("Copy number already exists for this book.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid copy status.");
    }

    public void SetBookCopyId(int bookCopyId)
    {
        _bookCopyId = bookCopyId;
    }

    public void SetBookId(int bookId)
    {
        _bookId = bookId;
    }

    private async Task<bool> BeUniqueCopyNumberAsync(string copyNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(copyNumber) || _bookId == 0)
            return true;

        return !await _unitOfWork.Repository<BookCopy>().ExistsAsync(
            bc => bc.CopyNumber == copyNumber && bc.BookId == _bookId && bc.Id != _bookCopyId);
    }
}