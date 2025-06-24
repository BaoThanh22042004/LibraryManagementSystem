using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Queries;

public record GetCategoryByNameQuery(string Name) : IRequest<CategoryDto?>;

public class GetCategoryByNameQueryHandler : IRequestHandler<GetCategoryByNameQuery, CategoryDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoryByNameQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CategoryDto?> Handle(GetCategoryByNameQuery request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        var category = await categoryRepository.GetAsync(
            c => c.Name.ToLower() == request.Name.ToLower(),
            c => c.Books
        );

        if (category == null)
            return null;

        return _mapper.Map<CategoryDto>(category);
    }
}