using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record FulfillReservationCommand(int Id, int BookCopyId) : IRequest<Result>;

public class FulfillReservationCommandHandler : IRequestHandler<FulfillReservationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public FulfillReservationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(FulfillReservationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            
            // Get reservation with book details
            var reservation = await reservationRepository.GetAsync(
                r => r.Id == request.Id,
                r => r.Book
            );
            
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found.");
            
            // Only allow fulfillment if the reservation is active
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure($"Only active reservations can be fulfilled. Current status: {reservation.Status}");
            
            // Get book copy
            var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.BookCopyId);
            if (bookCopy == null)
                return Result.Failure($"Book copy with ID {request.BookCopyId} not found.");
            
            // Check if book copy is for the correct book
            if (bookCopy.BookId != reservation.BookId)
                return Result.Failure($"Book copy with ID {request.BookCopyId} is not for the reserved book.");
            
            // Check if book copy is available
            if (bookCopy.Status != CopyStatus.Available)
                return Result.Failure($"Book copy is not available. Current status: {bookCopy.Status}");
            
            // Update reservation status to fulfilled
            reservation.Status = ReservationStatus.Fulfilled;
            
            // Update book copy status to reserved
            bookCopy.Status = CopyStatus.Reserved;
            
            reservationRepository.Update(reservation);
            bookCopyRepository.Update(bookCopy);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to fulfill reservation: {ex.Message}");
        }
    }
}