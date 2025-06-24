using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

public record GetFinesByLoanIdQuery(int LoanId) : IRequest<List<FineDto>>;

public class GetFinesByLoanIdQueryHandler : IRequestHandler<GetFinesByLoanIdQuery, List<FineDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetFinesByLoanIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<FineDto>> Handle(GetFinesByLoanIdQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        var fines = await fineRepository.ListAsync(
            predicate: f => f.LoanId == request.LoanId,
            orderBy: q => q.OrderByDescending(f => f.FineDate),
            asNoTracking: true,
            f => f.Member,
            f => f.Loan!
        );
        
        var fineDtos = _mapper.Map<List<FineDto>>(fines);
        
        // Add member names to the DTOs
        for (int i = 0; i < fines.Count; i++)
        {
            fineDtos[i].MemberName = fines[i].Member?.User?.FullName ?? "Unknown";
        }
        
        return fineDtos;
    }
}