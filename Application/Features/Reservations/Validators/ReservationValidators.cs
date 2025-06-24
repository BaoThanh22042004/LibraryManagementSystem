using Application.Features.Reservations.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Reservations.Validators
{
	public class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
	{
		public CancelReservationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Valid Reservation ID is required");
		}
	}

	public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
	{
		public CreateReservationCommandValidator()
		{
			RuleFor(x => x.ReservationDto.MemberId)
				.GreaterThan(0)
				.WithMessage("Valid Member ID is required");

			RuleFor(x => x.ReservationDto.BookId)
				.GreaterThan(0)
				.WithMessage("Valid Book ID is required");
		}
	}

	public class DeleteReservationCommandValidator : AbstractValidator<DeleteReservationCommand>
	{
		public DeleteReservationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Valid Reservation ID is required");
		}
	}

	public class FulfillReservationCommandValidator : AbstractValidator<FulfillReservationCommand>
	{
		public FulfillReservationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Valid Reservation ID is required");

			RuleFor(x => x.BookCopyId)
				.GreaterThan(0)
				.WithMessage("Valid Book Copy ID is required");
		}
	}

	public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
	{
		public UpdateReservationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0)
				.WithMessage("Valid Reservation ID is required");

			RuleFor(x => x.ReservationDto.Status)
				.IsInEnum()
				.WithMessage("Valid Reservation Status is required");

			When(x => x.ReservationDto.BookCopyId.HasValue, () => {
				RuleFor(x => x.ReservationDto.BookCopyId!.Value)
					.GreaterThan(0)
					.WithMessage("Valid Book Copy ID is required");
			});
		}
	}
}
