using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for CreateCategoryDto
/// Enforces business rules for category creation
/// </summary>
public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .Length(1, 100).WithMessage("Category name must be between 1 and 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for UpdateCategoryDto
/// Enforces business rules for category updates
/// </summary>
public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid category ID");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .Length(1, 100).WithMessage("Category name must be between 1 and 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for CategorySearchParametersDto
/// Enforces valid search parameters
/// </summary>
public class CategorySearchRequestValidator : AbstractValidator<CategorySearchRequest>
{
    public CategorySearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100 items");
    }
}