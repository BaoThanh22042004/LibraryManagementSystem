using Application.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators;

/// <summary>
/// Validator for updating user's own profile (UC003 - Update Profile).
/// </summary>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private readonly Regex _phoneRegex = new(@"^\+?[0-9\s\-\(\)]{5,20}$");

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Invalid user ID");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Full name contains invalid characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () => {
            RuleFor(x => x.Phone)
                .Matches(_phoneRegex).WithMessage("Phone number format is invalid")
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Address), () => {
            RuleFor(x => x.Address)
                .MaximumLength(255).WithMessage("Address cannot exceed 255 characters");
        });
    }
}