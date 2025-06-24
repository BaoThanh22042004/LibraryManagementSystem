using Application.Features.Users.Commands;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Features.Users.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserDto.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");

        RuleFor(x => x.UserDto.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.UserDto.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.UserDto.Role)
            .IsInEnum().WithMessage("Invalid role");
    }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.UserDto.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");

        RuleFor(x => x.UserDto.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.UserDto.Role)
            .IsInEnum().WithMessage("Invalid role");
    }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.PasswordDto.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.PasswordDto.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character")
            .NotEqual(x => x.PasswordDto.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.PasswordDto.ConfirmNewPassword)
            .Equal(x => x.PasswordDto.NewPassword).WithMessage("Confirmation password does not match new password");
    }
}

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.LoginDto.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.LoginDto.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}