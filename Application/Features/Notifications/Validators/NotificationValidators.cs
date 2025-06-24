using Application.Features.Notifications.Commands;
using FluentValidation;

namespace Application.Features.Notifications.Validators
{
	public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
	{
		public CreateNotificationCommandValidator()
		{
			RuleFor(x => x.NotificationDto.Subject)
				.NotEmpty().WithMessage("Subject is required")
				.MaximumLength(200).WithMessage("Subject cannot exceed 200 characters");

			RuleFor(x => x.NotificationDto.Message)
				.NotEmpty().WithMessage("Message is required")
				.MaximumLength(500).WithMessage("Message cannot exceed 500 characters");

			RuleFor(x => x.NotificationDto.Type)
				.IsInEnum().WithMessage("Invalid notification type");

			When(x => x.NotificationDto.UserId.HasValue, () =>
			{
				RuleFor(x => x.NotificationDto.UserId!.Value)
					.GreaterThan(0).WithMessage("User ID must be greater than 0");
			});
		}
	}

	public class DeleteNotificationCommandValidator : AbstractValidator<DeleteNotificationCommand>
	{
		public DeleteNotificationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0).WithMessage("Notification ID must be greater than 0");
		}
	}

	public class MarkAllAsReadCommandValidator : AbstractValidator<MarkAllAsReadCommand>
	{
		public MarkAllAsReadCommandValidator()
		{
			RuleFor(x => x.UserId)
				.GreaterThan(0).WithMessage("User ID must be greater than 0");
		}
	}

	public class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
	{
		public MarkAsReadCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0).WithMessage("Notification ID must be greater than 0");
		}
	}

	public class UpdateNotificationCommandValidator : AbstractValidator<UpdateNotificationCommand>
	{
		public UpdateNotificationCommandValidator()
		{
			RuleFor(x => x.Id)
				.GreaterThan(0).WithMessage("Notification ID must be greater than 0");

			RuleFor(x => x.NotificationDto.Status)
				.IsInEnum().WithMessage("Invalid notification status");
		}
	}
}
