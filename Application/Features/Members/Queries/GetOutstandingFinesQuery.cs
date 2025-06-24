using Application.Interfaces;
using MediatR;

namespace Application.Features.Members.Queries;

public record GetOutstandingFinesQuery(int MemberId) : IRequest<decimal>;

public class GetOutstandingFinesQueryHandler : IRequestHandler<GetOutstandingFinesQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOutstandingFinesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> Handle(GetOutstandingFinesQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Domain.Entities.Fine>();
        
        var unpaidFines = await fineRepository.ListAsync(
            predicate: f => f.MemberId == request.MemberId && f.Status == Domain.Enums.FineStatus.Pending
        );

        return unpaidFines.Sum(f => f.Amount);
    }
}