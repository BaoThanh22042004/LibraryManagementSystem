using Application.Features.Members.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Members.Validators;

public class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.MemberDto.MembershipNumber)
            .NotEmpty().WithMessage("Membership number is required")
            .MaximumLength(20).WithMessage("Membership number cannot exceed 20 characters")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Membership number can only contain letters, numbers, and hyphens");

        RuleFor(x => x.MemberDto.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
}

public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Member ID must be greater than 0");

        RuleFor(x => x.MemberDto.MembershipStatus)
            .IsInEnum().WithMessage("Invalid membership status");

        RuleFor(x => x.MemberDto.MembershipStartDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Membership start date cannot be in the future")
            .When(x => x.MemberDto.MembershipStartDate.HasValue);
    }
}

public class UpdateMembershipStatusCommandValidator : AbstractValidator<UpdateMembershipStatusCommand>
{
    public UpdateMembershipStatusCommandValidator()
    {
        RuleFor(x => x.MemberId)
            .GreaterThan(0).WithMessage("Member ID must be greater than 0");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid membership status");
    }
}

public class DeleteMemberCommandValidator : AbstractValidator<DeleteMemberCommand>
{
    public DeleteMemberCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Member ID must be greater than 0");
    }
}