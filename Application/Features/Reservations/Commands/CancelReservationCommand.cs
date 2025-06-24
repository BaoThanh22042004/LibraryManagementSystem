using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record CancelReservationCommand(int Id) : IRequest<Result>;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelReservationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get reservation
            var reservation = await reservationRepository.GetAsync(r => r.Id == request.Id);
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found.");
            
            // Only allow cancellation if the reservation is active
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure($"Only active reservations can be cancelled. Current status: {reservation.Status}");
            
            // Update status to cancelled
            reservation.Status = ReservationStatus.Cancelled;
            
            reservationRepository.Update(reservation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to cancel reservation: {ex.Message}");
        }
    }
}