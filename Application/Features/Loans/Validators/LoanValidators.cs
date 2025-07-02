using Application.DTOs;
using Application.Features.Loans.Commands;
using FluentValidation;

namespace Application.Features.Loans.Validators;

public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.LoanDto.MemberId)
            .GreaterThan(0).WithMessage("Member ID must be greater than 0");

        RuleFor(x => x.LoanDto.BookCopyId)
            .GreaterThan(0).WithMessage("Book Copy ID must be greater than 0");

        // CustomDueDate is optional but must be in the future if provided
        RuleFor(x => x.LoanDto.CustomDueDate)
            .Must(dueDate => dueDate == null || dueDate > DateTime.Now)
            .WithMessage("Due date must be in the future");
    }
}

public class CreateLoanDtoValidator : AbstractValidator<CreateLoanDto>
{
    public CreateLoanDtoValidator() 
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).WithMessage("Member ID must be greater than 0");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("Book Copy ID must be greater than 0");

        // CustomDueDate is optional but must be in the future if provided
        RuleFor(x => x.CustomDueDate)
            .Must(dueDate => dueDate == null || dueDate > DateTime.Now)
            .WithMessage("Due date must be in the future");
    }
}

public class UpdateLoanCommandValidator : AbstractValidator<UpdateLoanCommand>
{
    public UpdateLoanCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Loan ID must be greater than 0");

        RuleFor(x => x.LoanDto.Status)
            .IsInEnum().WithMessage("Invalid loan status");

        RuleFor(x => x.LoanDto.ReturnDate)
            .Must((dto, returnDate) => !returnDate.HasValue || returnDate.Value <= DateTime.Now)
            .WithMessage("Return date cannot be in the future");
    }
}

public class ExtendLoanCommandValidator : AbstractValidator<ExtendLoanCommand>
{
    public ExtendLoanCommandValidator()
    {
        RuleFor(x => x.ExtendLoanDto.LoanId)
            .GreaterThan(0).WithMessage("Loan ID must be greater than 0");

        RuleFor(x => x.ExtendLoanDto.NewDueDate)
            .NotEmpty().WithMessage("New due date is required")
            .GreaterThan(DateTime.Now).WithMessage("New due date must be in the future");
    }
}

public class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0).WithMessage("Loan ID must be greater than 0");
    }
}