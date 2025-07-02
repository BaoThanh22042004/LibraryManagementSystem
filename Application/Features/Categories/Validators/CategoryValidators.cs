using Application.Features.Categories.Commands;
using Application.Features.Categories.Queries;
using FluentValidation;

namespace Application.Features.Categories.Validators;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryDto.Name)
            .NotEmpty().WithMessage("Category name is required")
			.MaximumLength(100).WithMessage("Category name must not exceed 100 characters");

		RuleFor(x => x.CategoryDto.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

		RuleFor(x => x.CategoryDto.CoverImageUrl)
            .MaximumLength(255).WithMessage("Cover image URL must not exceed 255 characters");
	}
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Category ID must be greater than 0");

		RuleFor(x => x.CategoryDto.Name)
            .NotEmpty().WithMessage("Category name is required")
			.MaximumLength(100).WithMessage("Category name must not exceed 100 characters");

		RuleFor(x => x.CategoryDto.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

		RuleFor(x => x.CategoryDto.CoverImageUrl)
            .MaximumLength(255).WithMessage("Cover image URL must not exceed 255 characters");
	}
}

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Category ID must be greater than 0");
	}
}

public class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Category ID must be greater than 0");
    }
}

public class GetCategoryByNameQueryValidator : AbstractValidator<GetCategoryByNameQuery>
{
    public GetCategoryByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
    }
}

public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");
    }
}