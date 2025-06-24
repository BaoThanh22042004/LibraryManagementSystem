using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Queries;

public record GetAllCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        var categories = await categoryRepository.ListAsync(
            orderBy: q => q.OrderBy(c => c.Name),
            includes: new System.Linq.Expressions.Expression<Func<Category, object>>[] { c => c.Books }
        );

        return _mapper.Map<List<CategoryDto>>(categories);
    }
}