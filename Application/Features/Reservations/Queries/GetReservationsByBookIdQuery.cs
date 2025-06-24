using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reservations.Queries;

public record GetReservationsByBookIdQuery(int BookId) : IRequest<List<ReservationDto>>;

public class GetReservationsByBookIdQueryHandler : IRequestHandler<GetReservationsByBookIdQuery, List<ReservationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetReservationsByBookIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ReservationDto>> Handle(GetReservationsByBookIdQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        var reservations = await reservationRepository.ListAsync(
            predicate: r => r.BookId == request.BookId,
            orderBy: q => q.OrderByDescending(r => r.ReservationDate),
            asNoTracking: true,
            r => r.Member.User,
            r => r.Book
        );
        
        var reservationDtos = _mapper.Map<List<ReservationDto>>(reservations);
        
        // Set additional properties from navigation properties
        for (int i = 0; i < reservations.Count; i++)
        {
            reservationDtos[i].MemberName = reservations[i].Member?.User?.FullName ?? string.Empty;
            reservationDtos[i].BookTitle = reservations[i].Book?.Title ?? string.Empty;
        }
        
        return reservationDtos;
    }
}