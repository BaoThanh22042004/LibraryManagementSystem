using Application.DTOs;
using FluentValidation;
using Domain.Enums;

namespace Application.Validators;

/// <summary>
/// Validator for CreateBookCopyDto
/// Enforces business rules for book copy creation
/// </summary>
public class CreateBookCopyRequestValidator : AbstractValidator<CreateBookCopyRequest>
{
    public CreateBookCopyRequestValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Invalid book ID");

        RuleFor(x => x.CopyNumber)
            .MaximumLength(50).WithMessage("Copy number cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CopyNumber));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid copy status");
    }
}

/// <summary>
/// Validator for CreateMultipleBookCopiesDto
/// Enforces business rules for bulk copy creation
/// </summary>
public class CreateMultipleBookCopiesRequestValidator : AbstractValidator<CreateMultipleBookCopiesRequest>
{
    public CreateMultipleBookCopiesRequestValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Invalid book ID");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Cannot create more than 100 copies at once");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid copy status");
    }
}

/// <summary>
/// Validator for UpdateBookCopyStatusDto
/// Enforces business rules for copy status updates
/// </summary>
public class UpdateBookCopyStatusRequestValidator : AbstractValidator<UpdateBookCopyStatusRequest>
{
    public UpdateBookCopyStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid copy ID");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid copy status")
            .NotEqual(CopyStatus.Borrowed).WithMessage("Cannot manually set status to Borrowed - use loan operations")
            .NotEqual(CopyStatus.Reserved).WithMessage("Cannot manually set status to Reserved - use reservation operations");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}