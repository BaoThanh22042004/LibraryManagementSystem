using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Fines.Queries;

public record GetFineByIdQuery(int Id) : IRequest<FineDto?>;

public class GetFineByIdQueryHandler : IRequestHandler<GetFineByIdQuery, FineDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetFineByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<FineDto?> Handle(GetFineByIdQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        var fine = await fineRepository.GetAsync(
            f => f.Id == request.Id,
            f => f.Member,
            f => f.Loan!
        );
        
        if (fine == null)
            return null;
            
        var fineDto = _mapper.Map<FineDto>(fine);
        fineDto.MemberName = fine.Member?.User?.FullName ?? "Unknown";
        
        return fineDto;
    }
}