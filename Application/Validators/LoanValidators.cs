using Application.DTOs;
using FluentValidation;
using Application.Interfaces;
using Domain.Enums;
using Application.Common;

namespace Application.Validators;

/// <summary>
/// Validator for CreateLoanDto - UC018 (Check Out)
/// Enforces business rules for loan creation
/// </summary>
public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).WithMessage("Invalid member ID");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("Invalid book copy ID");

        When(x => x.CustomDueDate.HasValue, () => {
            RuleFor(x => x.CustomDueDate)
                .Must(date => date > DateTime.UtcNow).WithMessage("Due date must be in the future")
                .Must(date => date <= DateTime.UtcNow.AddDays(30)).WithMessage("Due date cannot exceed 30 days from now");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for ReturnBookDto - UC019 (Return Book)
/// </summary>
public class ReturnBookRequestValidator : AbstractValidator<ReturnBookRequest>
{
    public ReturnBookRequestValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0).WithMessage("Invalid loan ID");

        RuleFor(x => x.BookCondition)
            .IsInEnum().WithMessage("Invalid book condition");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for RenewLoanDto - UC020 (Renew Loan)
/// </summary>
public class RenewLoanRequestValidator : AbstractValidator<RenewLoanRequest>
{
    public RenewLoanRequestValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0).WithMessage("Invalid loan ID");

        When(x => x.NewDueDate.HasValue, () => {
            RuleFor(x => x.NewDueDate)
                .Must(date => date > DateTime.UtcNow).WithMessage("New due date must be in the future");
        });

        RuleFor(x => x.RenewalReason)
            .MaximumLength(500).WithMessage("Renewal reason cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for LoanSearchParametersDto - UC021 (View Loan History)
/// </summary>
public class LoanSearchRequestValidator : AbstractValidator<LoanSearchRequest>
{
    public LoanSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");

        When(x => x.MemberId.HasValue, () => {
            RuleFor(x => x.MemberId)
                .GreaterThan(0).WithMessage("Member ID must be greater than 0");
        });

        When(x => x.BookCopyId.HasValue, () => {
            RuleFor(x => x.BookCopyId)
                .GreaterThan(0).WithMessage("Book copy ID must be greater than 0");
        });

        When(x => x.BookId.HasValue, () => {
            RuleFor(x => x.BookId)
                .GreaterThan(0).WithMessage("Book ID must be greater than 0");
        });

        When(x => x.Status.HasValue, () => {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid loan status");
        });

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () => {
            RuleFor(x => x)
                .Must(x => x.FromDate <= x.ToDate)
                .WithMessage("From date must be earlier than or equal to To date");
        });
    }
}

public class OverdueLoansReportRequestValidator : AbstractValidator<PagedRequest>
{
    public OverdueLoansReportRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        // Add role-based validation in controller/service as needed
    }
}