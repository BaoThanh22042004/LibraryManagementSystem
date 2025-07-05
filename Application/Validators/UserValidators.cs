using Application.DTOs;
using Domain.Enums;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators;

/// <summary>
/// Validator for the CreateUserRequest DTO (UC001 - Create User).
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private readonly Regex _passwordStrengthRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");
    private readonly Regex _phoneRegex = new(@"^\+?[0-9\s\-\(\)]{5,20}$");
    private readonly Regex _membershipNumberRegex = new(@"^[a-zA-Z0-9\-]{1,20}$");
    private readonly Regex _employeeIdRegex = new(@"^[a-zA-Z0-9\-]{1,20}$");

    public CreateUserRequestValidator()
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

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role")
            .NotEqual(UserRole.Member).Unless(x => x.Role == UserRole.Member).WithMessage("User role must be Member, Librarian, or Admin");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () => {
            RuleFor(x => x.Phone)
                .Matches(_phoneRegex).WithMessage("Phone number format is invalid")
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Address), () => {
            RuleFor(x => x.Address)
                .MaximumLength(255).WithMessage("Address cannot exceed 255 characters");
        });

        // Member-specific validation
        When(x => x.Role == UserRole.Member, () => {
            When(x => !string.IsNullOrWhiteSpace(x.MembershipNumber), () => {
                RuleFor(x => x.MembershipNumber)
                    .Matches(_membershipNumberRegex).WithMessage("Membership number can only contain letters, numbers, and hyphens")
                    .MaximumLength(20).WithMessage("Membership number cannot exceed 20 characters");
            });
        });

        // Librarian-specific validation
        When(x => x.Role == UserRole.Librarian, () => {
            When(x => !string.IsNullOrWhiteSpace(x.EmployeeId), () => {
                RuleFor(x => x.EmployeeId)
                    .Matches(_employeeIdRegex).WithMessage("Employee ID can only contain letters, numbers, and hyphens")
                    .MaximumLength(20).WithMessage("Employee ID cannot exceed 20 characters");
            });
        });
    }
}

/// <summary>
/// Validator for the UpdateUserRequest DTO (UC006 - Update User Info).
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    private readonly Regex _phoneRegex = new(@"^\+?[0-9\s\-\(\)]{5,20}$");

    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id)
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

        When(x => x.Status.HasValue, () => {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid user status");

            RuleFor(x => x.StatusChangeReason)
                .NotEmpty().WithMessage("Status change reason is required when changing status")
                .MaximumLength(500).WithMessage("Status change reason cannot exceed 500 characters");
        });
    }
}

/// <summary>
/// Validator for the UserSearchRequest DTO (UC007 - View User Info).
/// </summary>
public class UserSearchRequestValidator : AbstractValidator<UserSearchRequest>
{
    public UserSearchRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.SearchTerm), () => {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters");
        });

        When(x => x.Role.HasValue, () => {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid user role");
        });

        When(x => x.Status.HasValue, () => {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid user status");
        });

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}