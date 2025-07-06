using Application.DTOs;
using FluentValidation;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Validators;

/// <summary>
/// Validator for CreateFineDto - UC026 (Calculate Fine)
/// Enforces business rules for fine creation
/// </summary>
public class CreateFineRequestValidator : AbstractValidator<CreateFineRequest>
{
	public CreateFineRequestValidator()
	{
		RuleFor(x => x.MemberId)
			.GreaterThan(0).WithMessage("Invalid member ID");

		When(x => x.LoanId.HasValue, () =>
		{
			RuleFor(x => x.LoanId)
				.GreaterThan(0).WithMessage("Invalid loan ID");
		});

		RuleFor(x => x.Type)
			.IsInEnum().WithMessage("Invalid fine type");

		RuleFor(x => x.Amount)
			.GreaterThan(0).WithMessage("Fine amount must be greater than 0")
			.LessThanOrEqualTo(1000).WithMessage("Fine amount cannot exceed $1000");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Description is required")
			.MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
	}
}

/// <summary>
/// Validator for CalculateFineDto - UC026 (Calculate Fine)
/// </summary>
public class CalculateFineRequestValidator : AbstractValidator<CalculateFineRequest>
{
	public CalculateFineRequestValidator()
	{
		RuleFor(x => x.LoanId)
			.GreaterThan(0).WithMessage("Invalid loan ID");

		When(x => x.CustomDailyRate.HasValue, () =>
		{
			RuleFor(x => x.CustomDailyRate)
				.GreaterThan(0).WithMessage("Custom daily rate must be greater than 0")
				.LessThanOrEqualTo(10).WithMessage("Custom daily rate cannot exceed $10 per day");
		});

		When(x => x.MaximumFineAmount.HasValue, () =>
		{
			RuleFor(x => x.MaximumFineAmount)
				.GreaterThan(0).WithMessage("Maximum fine amount must be greater than 0")
				.LessThanOrEqualTo(1000).WithMessage("Maximum fine amount cannot exceed $1000");
		});

		RuleFor(x => x.AdditionalDescription)
			.MaximumLength(500).WithMessage("Additional description cannot exceed 500 characters");
	}
}

/// <summary>
/// Validator for PayFineDto - UC027 (Pay Fine)
/// </summary>
public class PayFineRequestValidator : AbstractValidator<PayFineRequest>
{
	public PayFineRequestValidator()
	{
		RuleFor(x => x.FineId)
			.GreaterThan(0).WithMessage("Invalid fine ID");

		RuleFor(x => x.PaymentAmount)
			.GreaterThan(0).WithMessage("Payment amount must be greater than 0")
			.LessThanOrEqualTo(1000).WithMessage("Payment amount cannot exceed $1000");

		RuleFor(x => x.PaymentMethod)
			.IsInEnum().WithMessage("Invalid payment method");

		RuleFor(x => x.PaymentReference)
			.MaximumLength(50).WithMessage("Payment reference cannot exceed 50 characters");

		RuleFor(x => x.Notes)
			.MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
	}
}

/// <summary>
/// Validator for WaiveFineDto - UC028 (Waive Fine)
/// </summary>
public class WaiveFineRequestValidator : AbstractValidator<WaiveFineRequest>
{
	public WaiveFineRequestValidator()
	{
		RuleFor(x => x.FineId)
			.GreaterThan(0).WithMessage("Invalid fine ID");

		RuleFor(x => x.StaffId)
			.GreaterThan(0).WithMessage("Invalid staff ID");

		RuleFor(x => x.WaiverReason)
			.NotEmpty().WithMessage("Waiver reason is required")
			.MaximumLength(500).WithMessage("Waiver reason cannot exceed 500 characters");
	}
}

/// <summary>
/// Validator for FineSearchParametersDto - UC029 (View Fine History)
/// </summary>
public class FineSearchRequestValidator : AbstractValidator<FineSearchRequest>
{
	public FineSearchRequestValidator()
	{
		RuleFor(x => x.Page)
			.GreaterThan(0).WithMessage("Page number must be greater than 0");

		RuleFor(x => x.PageSize)
			.GreaterThan(0).WithMessage("Page size must be greater than 0")
			.LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");

		When(x => x.MemberId.HasValue, () =>
		{
			RuleFor(x => x.MemberId)
				.GreaterThan(0).WithMessage("Member ID must be greater than 0");
		});

		When(x => x.LoanId.HasValue, () =>
		{
			RuleFor(x => x.LoanId)
				.GreaterThan(0).WithMessage("Loan ID must be greater than 0");
		});

		When(x => x.Type.HasValue, () =>
		{
			RuleFor(x => x.Type)
				.IsInEnum().WithMessage("Invalid fine type");
		});

		When(x => x.Status.HasValue, () =>
		{
			RuleFor(x => x.Status)
				.IsInEnum().WithMessage("Invalid fine status");
		});

		When(x => x.MinAmount.HasValue, () =>
		{
			RuleFor(x => x.MinAmount)
				.GreaterThanOrEqualTo(0).WithMessage("Minimum amount cannot be negative");
		});

		When(x => x.MaxAmount.HasValue, () =>
		{
			RuleFor(x => x.MaxAmount)
				.GreaterThan(0).WithMessage("Maximum amount must be greater than 0");
		});

		When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue, () =>
		{
			RuleFor(x => x)
				.Must(x => x.MinAmount <= x.MaxAmount)
				.WithMessage("Minimum amount must be less than or equal to maximum amount");
		});

		When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
		{
			RuleFor(x => x)
				.Must(x => x.FromDate <= x.ToDate)
				.WithMessage("From date must be earlier than or equal to To date");
		});
	}
}