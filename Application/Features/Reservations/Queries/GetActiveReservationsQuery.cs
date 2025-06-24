using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries;

public record GetActiveReservationsQuery : IRequest<List<ReservationDto>>;

public class GetActiveReservationsQueryHandler : IRequestHandler<GetActiveReservationsQuery, List<ReservationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveReservationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ReservationDto>> Handle(GetActiveReservationsQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        var activeReservations = await reservationRepository.ListAsync(
            predicate: r => r.Status == ReservationStatus.Active,
            orderBy: q => q.OrderBy(r => r.ReservationDate), // Oldest first, as they have priority
            asNoTracking: true,
            r => r.Member.User,
            r => r.Book
        );
        
        var reservationDtos = _mapper.Map<List<ReservationDto>>(activeReservations);
        
        // Set additional properties from navigation properties
        for (int i = 0; i < activeReservations.Count; i++)
        {
            reservationDtos[i].MemberName = activeReservations[i].Member?.User?.FullName ?? string.Empty;
            reservationDtos[i].BookTitle = activeReservations[i].Book?.Title ?? string.Empty;
        }
        
        return reservationDtos;
    }
}