using Application.Features.Fines.Commands;
using FluentValidation;

namespace Application.Features.Fines.Validators
{
	public class CreateFineCommandValidator : AbstractValidator<CreateFineCommand>
	{
		public CreateFineCommandValidator()
		{
			RuleFor(x => x.FineDto.Amount)
				.GreaterThan(0)
				.WithMessage("Fine amount must be greater than 0");

			RuleFor(x => x.FineDto.Description)
				.NotEmpty()
				.WithMessage("Description is required")
				.MaximumLength(500)
				.WithMessage("Description cannot exceed 500 characters");

			RuleFor(x => x.FineDto.Type)
				.IsInEnum()
				.WithMessage("Invalid fine type");

			RuleFor(x => x.FineDto.MemberId)
				.GreaterThan(0)
				.WithMessage("Member ID is required");
		}
	}

	public class DeleteFineCommandValidator : AbstractValidator<DeleteFineCommand>
	{
		public DeleteFineCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Fine ID is required");
		}
	}

	public class PayFineCommandValidator : AbstractValidator<PayFineCommand>
	{
		public PayFineCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Fine ID is required");
		}
	}

	public class UpdateFineCommandValidator : AbstractValidator<UpdateFineCommand>
	{
		public UpdateFineCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Fine ID is required");

			RuleFor(x => x.FineDto.Description)
				.MaximumLength(500)
				.WithMessage("Description cannot exceed 500 characters");

			RuleFor(x => x.FineDto.Status)
				.IsInEnum()
				.WithMessage("Invalid fine status");
		}
	}

	public class WaiveFineCommandValidator : AbstractValidator<WaiveFineCommand>
	{
		public WaiveFineCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Fine ID is required");
		}
	}
	
	public class CalculateAndCreateFineCommandValidator : AbstractValidator<CalculateAndCreateFineCommand>
	{
		public CalculateAndCreateFineCommandValidator()
		{
			RuleFor(x => x.LoanId)
				.GreaterThan(0)
				.WithMessage("Loan ID is required");
		}
	}
}
