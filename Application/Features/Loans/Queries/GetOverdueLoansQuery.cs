using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetOverdueLoansQuery : IRequest<List<LoanDto>>;

public class GetOverdueLoansQueryHandler : IRequestHandler<GetOverdueLoansQuery, List<LoanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetOverdueLoansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<LoanDto>> Handle(GetOverdueLoansQuery request, CancellationToken cancellationToken)
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

        return _mapper.Map<List<LoanDto>>(loans);
    }
}