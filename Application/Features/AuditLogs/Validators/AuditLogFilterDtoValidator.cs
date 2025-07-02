using Application.DTOs;
using FluentValidation;

namespace Application.Features.AuditLogs.Validators;

/// <summary>
/// Validator for the AuditLogFilterDto
/// </summary>
public class AuditLogFilterDtoValidator : AbstractValidator<AuditLogFilterDto>
{
    public AuditLogFilterDtoValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId.HasValue)
            .WithMessage("User ID must be greater than 0.");
            
        RuleFor(x => x.EntityType)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.EntityType))
            .WithMessage("Entity type cannot exceed 100 characters.");
            
        RuleFor(x => x.EntityId)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.EntityId))
            .WithMessage("Entity ID cannot exceed 50 characters.");
            
        RuleFor(x => x.SearchTerm)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage("Search term cannot exceed 255 characters.");
            
        RuleFor(x => x.Module)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Module))
            .WithMessage("Module name cannot exceed 100 characters.");
            
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after or equal to start date.");
            
        RuleFor(x => x.ActionTypes)
            .Must(types => types == null || types.Length <= 10)
            .WithMessage("Cannot filter by more than 10 action types at once.");
    }
}