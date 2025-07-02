using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Commands;

public record UpdateCategoryCommand(int Id, UpdateCategoryDto CategoryDto) : IRequest<Result>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();

		// Get the existing category by ID
		var category = await categoryRepository.GetAsync(c => c.Id == request.Id);
        
        if (category == null)
        {
            return Result.Failure($"Category with ID {request.Id} not found.");
		}

		// Check if the category name has changed and if the new name already exists
		if (category.Name.ToLower() != request.CategoryDto.Name.ToLower())
        {
            var nameExists = await categoryRepository.ExistsAsync(
                c => c.Name.ToLower() == request.CategoryDto.Name.ToLower() && c.Id != request.Id
            );
            
            if (nameExists)
            {
                return Result.Failure($"Category with name '{request.CategoryDto.Name}' already exists.");
			}
        }

		// Update the category properties
		category.Name = request.CategoryDto.Name;
        category.Description = request.CategoryDto.Description;
        category.CoverImageUrl = request.CategoryDto.CoverImageUrl;
        category.LastModifiedAt = DateTime.UtcNow;
        
        categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}