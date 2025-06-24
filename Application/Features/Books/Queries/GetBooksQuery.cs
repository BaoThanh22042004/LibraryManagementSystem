using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Books.Queries;

public record GetBooksQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<BookDto>>;

public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, PagedResult<BookDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBooksQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookDto>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var bookRepository = _unitOfWork.Repository<Book>();
        
        var bookQuery = await bookRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? b => b.Title.Contains(request.SearchTerm) || b.Author.Contains(request.SearchTerm) || b.ISBN.Contains(request.SearchTerm)
                : null,
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