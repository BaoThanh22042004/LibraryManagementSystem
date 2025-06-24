using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.BookCopies.Queries;

public record GetBookCopiesQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<BookCopyDto>>;

public class GetBookCopiesQueryHandler : IRequestHandler<GetBookCopiesQuery, PagedResult<BookCopyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookCopiesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookCopyDto>> Handle(GetBookCopiesQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        var bookCopyQuery = await bookCopyRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? bc => bc.CopyNumber.Contains(request.SearchTerm) || 
                       bc.Book.Title.Contains(request.SearchTerm) ||
                       bc.Book.ISBN.Contains(request.SearchTerm)
                : null,
            orderBy: q => q.OrderBy(bc => bc.Book.Title).ThenBy(bc => bc.CopyNumber),
            includes: new Expression<Func<BookCopy, object>>[] { bc => bc.Book }
        );

        return new PagedResult<BookCopyDto>(
            _mapper.Map<List<BookCopyDto>>(bookCopyQuery.Items),
            bookCopyQuery.TotalCount,
            bookCopyQuery.PageNumber,
            bookCopyQuery.PageSize
        );
    }
}