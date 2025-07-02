using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries;

public record GetReservationCountForMemberQuery(int MemberId) : IRequest<int>;

public class GetReservationCountForMemberQueryHandler : IRequestHandler<GetReservationCountForMemberQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReservationCountForMemberQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GetReservationCountForMemberQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        // Count active reservations for the member
        return await reservationRepository.CountAsync(r => 
            r.MemberId == request.MemberId && 
            r.Status == ReservationStatus.Active);
    }
}