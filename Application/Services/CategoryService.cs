using Application.Common;
using Application.DTOs;
using Application.Features.Categories.Commands;
using Application.Features.Categories.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<CategoryDto>> GetPaginatedCategoriesAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetCategoriesQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<CategoryDetailsDto?> GetCategoryByIdAsync(int id)
    {
        return await _mediator.Send(new GetCategoryByIdQuery(id));
    }

    public async Task<CategoryDto?> GetCategoryByNameAsync(string name)
    {
        return await _mediator.Send(new GetCategoryByNameQuery(name));
    }

    public async Task<int> CreateCategoryAsync(CreateCategoryDto categoryDto)
    {
        var result = await _mediator.Send(new CreateCategoryCommand(categoryDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, categoryDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        return await categoryRepository.ExistsAsync(c => c.Id == id);
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        return await categoryRepository.ExistsAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        return await _mediator.Send(new GetAllCategoriesQuery());
    }
}