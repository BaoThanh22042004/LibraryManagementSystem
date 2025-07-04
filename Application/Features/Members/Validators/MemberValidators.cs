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

public class SignUpMemberCommandValidator : AbstractValidator<SignUpMemberCommand>
{
    public SignUpMemberCommandValidator()
    {
        RuleFor(x => x.SignUpDto.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");

        RuleFor(x => x.SignUpDto.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.SignUpDto.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.SignUpDto.MembershipNumber)
            .MaximumLength(20).WithMessage("Membership number cannot exceed 20 characters")
            .Matches("^[A-Za-z0-9-]*$").WithMessage("Membership number can only contain letters, numbers, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.SignUpDto.MembershipNumber));
    }
}