using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Loans.Queries;

public record GetLoansByBookCopyIdQuery(int BookCopyId) : IRequest<List<LoanDto>>;

public class GetLoansByBookCopyIdQueryHandler : IRequestHandler<GetLoansByBookCopyIdQuery, List<LoanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoansByBookCopyIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<LoanDto>> Handle(GetLoansByBookCopyIdQuery request, CancellationToken cancellationToken)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        var loans = await loanRepository.ListAsync(
            predicate: l => l.BookCopyId == request.BookCopyId,
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