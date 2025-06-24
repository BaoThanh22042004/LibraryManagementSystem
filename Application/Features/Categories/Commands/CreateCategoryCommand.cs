using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Commands;

public record CreateCategoryCommand(CreateCategoryDto CategoryDto) : IRequest<Result<int>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();

		// Check if category name already exists
		var nameExists = await categoryRepository.ExistsAsync(c => c.Name.ToLower() == request.CategoryDto.Name.ToLower());
        if (nameExists)
        {
            return Result.Failure<int>($"Category with name '{request.CategoryDto.Name}' already exists.");
		}

		// Map DTO to entity
		var category = _mapper.Map<Category>(request.CategoryDto);
        
        await categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(category.Id);
    }
}