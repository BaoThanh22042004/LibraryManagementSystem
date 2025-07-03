using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries;

/// <summary>
/// Query to retrieve the next active reservation for a specific book (UC025 - Get Next Reservation).
/// </summary>
/// <remarks>
/// This implementation supports UC025 and UC024 specifications:
/// - Retrieves the oldest active reservation for a specific book
/// - Used to determine which member is next in the reservation queue
/// - Supports the reservation fulfillment process
/// - Returns null if no active reservations exist for the book
/// </remarks>
public record GetNextActiveReservationQuery(int BookId) : IRequest<ReservationDto?>;

public class GetNextActiveReservationQueryHandler : IRequestHandler<GetNextActiveReservationQuery, ReservationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetNextActiveReservationQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ReservationDto?> Handle(GetNextActiveReservationQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        // Get the oldest active reservation for this book
        var reservations = await reservationRepository.ListAsync(
            predicate: r => r.BookId == request.BookId && r.Status == ReservationStatus.Active,
            orderBy: q => q.OrderBy(r => r.ReservationDate),
            asNoTracking: true,
            includes: [r => r.Member.User,
            r => r.Book]
        );
        
        var reservation = reservations.FirstOrDefault();
        
        if (reservation == null)
            return null;
        
        var reservationDto = _mapper.Map<ReservationDto>(reservation);
        
        // Set additional properties from navigation properties
        reservationDto.MemberName = reservation.Member?.User?.FullName ?? string.Empty;
        reservationDto.BookTitle = reservation.Book?.Title ?? string.Empty;
        
        return reservationDto;
    }
}