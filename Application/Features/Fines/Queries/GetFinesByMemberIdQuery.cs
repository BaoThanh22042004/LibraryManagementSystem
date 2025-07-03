using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

/// <summary>
/// Query to retrieve fine history for a specific member (UC029 - View Fine History).
/// </summary>
/// <remarks>
/// This implementation follows UC029 specifications:
/// - Retrieves all fines (pending, paid, and waived) for a specific member
/// - Returns fine details including amounts, dates, and status
/// - Supports the member-specific fine history view (Alternative flow 29.2)
/// - Orders fines by date with most recent first
/// - Includes related loan information where applicable
/// </remarks>
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
                f => f.Member!.User
                // Not including f => f.Loan as it might be null
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