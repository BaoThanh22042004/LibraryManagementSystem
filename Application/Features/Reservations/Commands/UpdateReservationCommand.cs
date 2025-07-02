using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record UpdateReservationCommand(int Id, UpdateReservationDto ReservationDto) : IRequest<Result>;

public class UpdateReservationCommandHandler : IRequestHandler<UpdateReservationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReservationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get reservation
            var reservation = await reservationRepository.GetAsync(r => r.Id == request.Id);
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found.");
            
            // Update reservation
            reservation.Status = request.ReservationDto.Status;
            
            // Update BookCopyId if provided
            if (request.ReservationDto.BookCopyId.HasValue)
            {
                // Validate book copy exists
                var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
                var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.ReservationDto.BookCopyId.Value);
                
                if (bookCopy == null)
                    return Result.Failure($"Book copy with ID {request.ReservationDto.BookCopyId.Value} not found.");
                
                // Ensure book copy is for the correct book
                if (bookCopy.BookId != reservation.BookId)
                    return Result.Failure($"Book copy with ID {request.ReservationDto.BookCopyId.Value} is not for the reserved book.");
                
                reservation.BookCopyId = request.ReservationDto.BookCopyId;
            }
            
            reservationRepository.Update(reservation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update reservation: {ex.Message}");
        }
    }
}