using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Categories.Queries;

public record GetCategoriesQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var categoryRepository = _unitOfWork.Repository<Category>();
        
        var categoryQuery = await categoryRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? c => c.Name.Contains(request.SearchTerm) || 
                    (c.Description != null && c.Description.Contains(request.SearchTerm))
                : null,
            orderBy: q => q.OrderBy(c => c.Name),
            includes: new Expression<Func<Category, object>>[] { c => c.Books }
        );

        return new PagedResult<CategoryDto>(
            _mapper.Map<List<CategoryDto>>(categoryQuery.Items),
            categoryQuery.TotalCount,
            categoryQuery.PageNumber,
            categoryQuery.PageSize
        );
    }
}