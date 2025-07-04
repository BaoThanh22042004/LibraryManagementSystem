using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetOverdueLoansQuery(PagedRequest PagedRequest) : IRequest<Result<PagedResult<LoanDto>>>;

    public class GetOverdueLoansQueryHandler : IRequestHandler<GetOverdueLoansQuery, Result<PagedResult<LoanDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetOverdueLoansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<LoanDto>>> Handle(GetOverdueLoansQuery request, CancellationToken cancellationToken)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        var now = DateTime.Now;
        
        var loans = await loanRepository.ListAsync(
            predicate: l => (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue) && l.DueDate < now,
            orderBy: q => q.OrderBy(l => l.DueDate), // Oldest overdue first
            includes: new Expression<Func<Loan, object>>[] { 
                l => l.Member, 
                l => l.Member.User,
                l => l.BookCopy,
                l => l.BookCopy.Book
            }
        );

        return Result.Success(new PagedResult<LoanDto>(loans.Select(l => _mapper.Map<LoanDto>(l)).ToList(), request.PagedRequest.PageNumber, request.PagedRequest.PageSize, loans.Count));
    }
}