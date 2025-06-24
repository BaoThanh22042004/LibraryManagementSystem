using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reservations.Queries;

public record GetReservationByIdQuery(int Id) : IRequest<ReservationDto?>;

public class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, ReservationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetReservationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ReservationDto?> Handle(GetReservationByIdQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        var reservation = await reservationRepository.GetAsync(
            r => r.Id == request.Id,
            r => r.Member.User,
            r => r.Book
        );
        
        if (reservation == null)
            return null;
        
        var reservationDto = _mapper.Map<ReservationDto>(reservation);
        
        // Set additional properties from navigation properties
        reservationDto.MemberName = reservation.Member?.User?.FullName ?? string.Empty;
        reservationDto.BookTitle = reservation.Book?.Title ?? string.Empty;
        
        return reservationDto;
    }
}