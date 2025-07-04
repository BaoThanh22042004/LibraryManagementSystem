using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Loans.Queries;

public record GetLoanByIdQuery(int Id) : IRequest<Result<LoanDto>>;

public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, Result<LoanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoanByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<LoanDto>> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        var loan = await loanRepository.GetAsync(
            l => l.Id == request.Id,
            l => l.Member,
            l => l.Member.User,
            l => l.BookCopy,
            l => l.BookCopy.Book
        );

        if (loan == null)
            return Result.Failure<LoanDto>("Loan not found");

        return Result.Success(_mapper.Map<LoanDto>(loan));
    }
}