using Application.Features.Librarians.Commands;
using FluentValidation;

namespace Application.Features.Librarians.Validators
{
	public class CreateLibrarianCommandValidator : AbstractValidator<CreateLibrarianCommand>
	{
		public CreateLibrarianCommandValidator()
		{
			RuleFor(x => x.LibrarianDto.EmployeeId)
				.NotEmpty().WithMessage("Employee ID is required")
				.MaximumLength(20).WithMessage("Employee ID cannot exceed 20 characters");

			RuleFor(x => x.LibrarianDto.UserId)
				.GreaterThan(0).WithMessage("Valid User ID is required");

			RuleFor(x => x.LibrarianDto.HireDate)
				.NotEmpty().WithMessage("Hire date is required")
				.LessThanOrEqualTo(DateTime.Now).WithMessage("Hire date cannot be in the future");
		}
	}

	public class DeleteLibrarianCommandValidator : AbstractValidator<DeleteLibrarianCommand>
	{
		public DeleteLibrarianCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0).WithMessage("Valid Librarian ID is required");
		}
	}

	public class UpdateLibrarianCommandValidator : AbstractValidator<UpdateLibrarianCommand>
	{
		public UpdateLibrarianCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0).WithMessage("Valid Librarian ID is required");

			RuleFor(x => x.LibrarianDto.EmployeeId)
				.NotEmpty().WithMessage("Employee ID is required")
				.MaximumLength(20).WithMessage("Employee ID cannot exceed 20 characters");

			RuleFor(x => x.LibrarianDto.HireDate)
				.NotEmpty().WithMessage("Hire date is required")
				.LessThanOrEqualTo(DateTime.Now).WithMessage("Hire date cannot be in the future");
		}
	}
}
