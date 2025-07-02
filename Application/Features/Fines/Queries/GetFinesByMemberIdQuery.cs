using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

public record GetFinesByMemberIdQuery(int MemberId) : IRequest<List<FineDto>>;

public class GetFinesByMemberIdQueryHandler : IRequestHandler<GetFinesByMemberIdQuery, List<FineDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetFinesByMemberIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<FineDto>> Handle(GetFinesByMemberIdQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        var fines = await fineRepository.ListAsync(
            predicate: f => f.MemberId == request.MemberId,
            orderBy: q => q.OrderByDescending(f => f.FineDate),
            includes: new Expression<Func<Fine, object>>[] 
            { 
                f => f.Member,
                f => f.Loan
            }
        );
        
        var fineDtos = _mapper.Map<List<FineDto>>(fines);
        
        // Set member name for each fine
        foreach (var fineDto in fineDtos)
        {
            var fine = fines.First(f => f.Id == fineDto.Id);
            fineDto.MemberName = fine.Member?.User?.FullName ?? "Unknown";
        }
        
        return fineDtos;
    }
}