using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Commands;

public record DeleteCategoryCommand(int Id) : IRequest<Result>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();

		// Get the category by ID, including its books
		var category = await categoryRepository.GetAsync(
            c => c.Id == request.Id,
            c => c.Books
        );
        
        if (category == null)
        {
            return Result.Failure($"Category with ID {request.Id} not found.");
		}

		// Check if the category has any associated books
		if (category.Books.Any())
        {
            return Result.Failure($"Cannot delete category '{category.Name}' because it has associated books. Please remove the books first.");
		}

		// Delete the category
		categoryRepository.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}