using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class NotificationCreateDtoValidator : AbstractValidator<NotificationCreateDto>
{
    public NotificationCreateDtoValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters.");
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");
    }
}

public class NotificationBatchCreateDtoValidator : AbstractValidator<NotificationBatchCreateDto>
{
    public NotificationBatchCreateDtoValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull().WithMessage("UserIds is required.")
            .Must(list => list.Count > 0).WithMessage("At least one user must be specified.");
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters.");
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");
    }
}

public class NotificationUpdateStatusDtoValidator : AbstractValidator<NotificationUpdateStatusDto>
{
    public NotificationUpdateStatusDtoValidator()
    {
        RuleFor(x => x.NotificationId).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class NotificationMarkAsReadDtoValidator : AbstractValidator<NotificationMarkAsReadDto>
{
    public NotificationMarkAsReadDtoValidator()
    {
        RuleFor(x => x.NotificationIds)
            .NotNull().WithMessage("NotificationIds is required.")
            .Must(list => list.Count > 0).WithMessage("At least one notification must be specified.");
    }
}
