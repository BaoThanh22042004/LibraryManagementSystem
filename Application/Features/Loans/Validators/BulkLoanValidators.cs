using Application.DTOs;
using FluentValidation;

namespace Application.Features.Loans.Validators;

public class BulkCheckoutDtoValidator : AbstractValidator<BulkCheckoutDto>
{
    private const int MaxAllowedLoans = 5;
    
    public BulkCheckoutDtoValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0)
            .WithMessage("Member ID must be greater than zero.");
            
        RuleFor(x => x.BookCopyIds)
            .NotEmpty()
            .WithMessage("At least one book copy must be specified for checkout.");
            
        RuleFor(x => x.BookCopyIds)
            .Must(ids => ids.Count <= MaxAllowedLoans)
            .WithMessage($"Cannot checkout more than {MaxAllowedLoans} books at once.");
            
        RuleFor(x => x.CustomDueDate)
            .Must(dueDate => dueDate == null || dueDate > DateTime.Now)
            .WithMessage("Due date must be in the future.");
    }
}

public class BulkReturnDtoValidator : AbstractValidator<BulkReturnDto>
{
    public BulkReturnDtoValidator()
    {
        RuleFor(x => x.LoanIds)
            .NotEmpty()
            .WithMessage("At least one loan must be specified for return.");
    }
}

public class ExtendLoanDtoValidator : AbstractValidator<ExtendLoanDto>
{
    public ExtendLoanDtoValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0)
            .WithMessage("Loan ID must be greater than zero.");
            
        RuleFor(x => x.NewDueDate)
            .GreaterThan(DateTime.Now)
            .WithMessage("New due date must be in the future.");
    }
}