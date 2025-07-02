using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Books.Queries;

public record GetBooksByCategoryQuery(int CategoryId, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<BookDto>>;

public class GetBooksByCategoryQueryHandler : IRequestHandler<GetBooksByCategoryQuery, PagedResult<BookDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBooksByCategoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookDto>> Handle(GetBooksByCategoryQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var bookRepository = _unitOfWork.Repository<Book>();
        
        // Get category to verify it exists
        var categoryRepository = _unitOfWork.Repository<Category>();
        var categoryExists = await categoryRepository.ExistsAsync(c => c.Id == request.CategoryId);
        
        if (!categoryExists)
        {
            return new PagedResult<BookDto>([], 0, request.PageNumber, request.PageSize);
        }
        
        // Query books by category
        var bookQuery = await bookRepository.PagedListAsync(
            pagedRequest,
            predicate: b => b.Categories.Any(c => c.Id == request.CategoryId),
            orderBy: q => q.OrderBy(b => b.Title),
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );

        return new PagedResult<BookDto>(
            _mapper.Map<List<BookDto>>(bookQuery.Items),
            bookQuery.TotalCount,
            bookQuery.PageNumber,
            bookQuery.PageSize
        );
    }
}