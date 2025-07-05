using Application.DTOs;
using Domain.Enums;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators;

/// <summary>
/// Validator for the LoginRequest DTO (UC002 - Login).
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

/// <summary>
/// Validator for the ResetPasswordRequest DTO (UC005 - Reset Password).
/// </summary>
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

/// <summary>
/// Validator for the ConfirmResetPasswordRequest DTO (UC005 - Reset Password).
/// </summary>
public class ConfirmResetPasswordRequestValidator : AbstractValidator<ConfirmResetPasswordRequest>
{
    private readonly Regex _passwordStrengthRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

    public ConfirmResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(_passwordStrengthRegex).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

/// <summary>
/// Validator for the ChangePasswordRequest DTO (UC004 - Change Password).
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    private readonly Regex _passwordStrengthRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Invalid user ID");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(_passwordStrengthRegex).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

/// <summary>
/// Validator for the RegisterRequest DTO (UC008 - Register Member).
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private readonly Regex _passwordStrengthRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");
    private readonly Regex _phoneRegex = new(@"^\+?[0-9\s\-\(\)]{5,20}$");
    private readonly Regex _membershipNumberRegex = new(@"^[a-zA-Z0-9\-]{1,20}$");

    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Full name contains invalid characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(_passwordStrengthRegex).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () => {
            RuleFor(x => x.Phone)
                .Matches(_phoneRegex).WithMessage("Phone number format is invalid")
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Address), () => {
            RuleFor(x => x.Address)
                .MaximumLength(255).WithMessage("Address cannot exceed 255 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PreferredMembershipNumber), () => {
            RuleFor(x => x.PreferredMembershipNumber)
                .Matches(_membershipNumberRegex).WithMessage("Membership number can only contain letters, numbers, and hyphens")
                .MaximumLength(20).WithMessage("Membership number cannot exceed 20 characters");
        });
    }
}