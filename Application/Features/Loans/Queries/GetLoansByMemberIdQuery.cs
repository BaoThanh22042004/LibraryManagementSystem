using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetLoansByMemberIdQuery(int MemberId) : IRequest<List<LoanDto>>;

public class GetLoansByMemberIdQueryHandler : IRequestHandler<GetLoansByMemberIdQuery, List<LoanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoansByMemberIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<LoanDto>> Handle(GetLoansByMemberIdQuery request, CancellationToken cancellationToken)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        var loans = await loanRepository.ListAsync(
            predicate: l => l.MemberId == request.MemberId,
            orderBy: q => q.OrderByDescending(l => l.LoanDate),
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