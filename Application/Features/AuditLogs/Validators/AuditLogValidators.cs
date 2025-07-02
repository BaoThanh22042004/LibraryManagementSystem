using Application.Features.AuditLogs.Commands;
using FluentValidation;

namespace Application.Features.AuditLogs.Validators;

/// <summary>
/// Validator for the CreateAuditLogCommand
/// </summary>
public class CreateAuditLogCommandValidator : AbstractValidator<CreateAuditLogCommand>
{
    public CreateAuditLogCommandValidator()
    {
        RuleFor(x => x.AuditLogDto.ActionType)
            .IsInEnum()
            .WithMessage("Invalid action type.");
            
        RuleFor(x => x.AuditLogDto.EntityType)
            .NotEmpty()
            .WithMessage("Entity type is required.")
            .MaximumLength(100)
            .WithMessage("Entity type cannot exceed 100 characters.");
            
        RuleFor(x => x.AuditLogDto.Details)
            .NotEmpty()
            .WithMessage("Details are required.")
            .MaximumLength(1000)
            .WithMessage("Details cannot exceed 1000 characters.");
            
        RuleFor(x => x.AuditLogDto.EntityId)
            .MaximumLength(50)
            .WithMessage("Entity ID cannot exceed 50 characters.");
            
        RuleFor(x => x.AuditLogDto.EntityName)
            .MaximumLength(255)
            .WithMessage("Entity name cannot exceed 255 characters.");
            
        RuleFor(x => x.AuditLogDto.IpAddress)
            .MaximumLength(50)
            .WithMessage("IP address cannot exceed 50 characters.");
            
        RuleFor(x => x.AuditLogDto.Module)
            .MaximumLength(100)
            .WithMessage("Module name cannot exceed 100 characters.");
            
        RuleFor(x => x.AuditLogDto.ErrorMessage)
            .MaximumLength(500)
            .WithMessage("Error message cannot exceed 500 characters.")
            .When(x => !x.AuditLogDto.IsSuccess);
    }
}