using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetLoansQuery(PagedRequest PagedRequest, int? MemberId = null) : IRequest<Result<PagedResult<LoanDto>>>;

public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, Result<PagedResult<LoanDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<LoanDto>>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PagedRequest.PageNumber,
            PageSize = request.PagedRequest.PageSize
        };

        var loanRepository = _unitOfWork.Repository<Loan>();
        
        var loanQuery = await loanRepository.PagedListAsync(
            pagedRequest,
            predicate: request.MemberId.HasValue 
                ? l => l.MemberId == request.MemberId
                : null,
            orderBy: q => q.OrderByDescending(l => l.LoanDate),
            includes: new Expression<Func<Loan, object>>[] { 
                l => l.Member, 
                l => l.Member.User,
                l => l.BookCopy,
                l => l.BookCopy.Book
            }
        );

        return Result.Success(new PagedResult<LoanDto>(
            _mapper.Map<List<LoanDto>>(loanQuery.Items),
            request.PagedRequest.PageNumber,
            request.PagedRequest.PageSize,
            loanQuery.TotalCount
        ));
    }
}