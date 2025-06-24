using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

public record GetPendingFinesQuery : IRequest<List<FineDto>>;

public class GetPendingFinesQueryHandler : IRequestHandler<GetPendingFinesQuery, List<FineDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPendingFinesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<FineDto>> Handle(GetPendingFinesQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        var pendingFines = await fineRepository.ListAsync(
            predicate: f => f.Status == FineStatus.Pending,
            orderBy: q => q.OrderByDescending(f => f.FineDate),
            asNoTracking: true,
            f => f.Member.User,
            f => f.Loan!
        );
        
        var fineDtos = _mapper.Map<List<FineDto>>(pendingFines);
        
        // Add member names to the DTOs
        for (int i = 0; i < pendingFines.Count; i++)
        {
            fineDtos[i].MemberName = pendingFines[i].Member?.User?.FullName ?? "Unknown";
        }
        
        return fineDtos;
    }
}