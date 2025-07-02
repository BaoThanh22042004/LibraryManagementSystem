using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Queries;

public record HasReservationsForBookQuery(int BookId) : IRequest<bool>;

public class HasReservationsForBookQueryHandler : IRequestHandler<HasReservationsForBookQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public HasReservationsForBookQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(HasReservationsForBookQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        // Check if there are any active or fulfilled reservations for the book
        return await reservationRepository.ExistsAsync(r => 
            r.BookId == request.BookId && 
            (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled));
    }
}