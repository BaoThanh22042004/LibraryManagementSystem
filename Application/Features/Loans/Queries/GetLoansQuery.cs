using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetLoansQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<LoanDto>>;

public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PagedResult<LoanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<LoanDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var loanRepository = _unitOfWork.Repository<Loan>();
        
        var loanQuery = await loanRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? l => l.BookCopy.Book.Title.Contains(request.SearchTerm) || 
                      l.BookCopy.CopyNumber.Contains(request.SearchTerm) ||
                      l.Member.User.FullName.Contains(request.SearchTerm)
                : null,
            orderBy: q => q.OrderByDescending(l => l.LoanDate),
            includes: new Expression<Func<Loan, object>>[] { 
                l => l.Member, 
                l => l.Member.User,
                l => l.BookCopy,
                l => l.BookCopy.Book
            }
        );

        return new PagedResult<LoanDto>(
            _mapper.Map<List<LoanDto>>(loanQuery.Items),
            loanQuery.TotalCount,
            loanQuery.PageNumber,
            loanQuery.PageSize
        );
    }
}