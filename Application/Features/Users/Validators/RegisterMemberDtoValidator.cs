using Application.DTOs;
using FluentValidation;

namespace Application.Features.Users.Validators;

/// <summary>
/// Validator for RegisterMemberDto to implement UC008 requirements
/// </summary>
public class RegisterMemberDtoValidator : AbstractValidator<RegisterMemberDto>
{
    public RegisterMemberDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
            
        RuleFor(x => x.PreferredMembershipNumber)
            .MaximumLength(20).WithMessage("Membership number cannot exceed 20 characters.")
            .Matches("^[a-zA-Z0-9-]*$").WithMessage("Membership number can only contain letters, numbers, and hyphens.")
            .When(x => !string.IsNullOrEmpty(x.PreferredMembershipNumber));
    }
}