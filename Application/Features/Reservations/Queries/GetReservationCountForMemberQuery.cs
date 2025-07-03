using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries;

/// <summary>
/// Query to count active reservations for a member (Support for UC022 - Reserve Book).
/// </summary>
/// <remarks>
/// This implementation supports UC022 specifications:
/// - Counts active reservations for a specific member
/// - Used to enforce reservation limits (typically 3 active reservations per member)
/// - Ensures fair access to reservation system for all members
/// </remarks>
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