using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record DeleteReservationCommand(int Id) : IRequest<Result>;

public class DeleteReservationCommandHandler : IRequestHandler<DeleteReservationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReservationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get reservation
            var reservation = await reservationRepository.GetAsync(r => r.Id == request.Id);
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found.");
            
            // Only allow deletion if the reservation is not already fulfilled
            if (reservation.Status == ReservationStatus.Fulfilled)
                return Result.Failure($"Cannot delete a fulfilled reservation.");
            
            // Delete reservation
            reservationRepository.Delete(reservation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to delete reservation: {ex.Message}");
        }
    }
}