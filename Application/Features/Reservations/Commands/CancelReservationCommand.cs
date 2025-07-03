using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

/// <summary>
/// Command to cancel an existing reservation (UC023 - Cancel Reservation).
/// </summary>
/// <remarks>
/// This implementation follows UC023 specifications:
/// - Validates reservation exists and is in active status
/// - Updates reservation status to cancelled
/// - Adjusts queue positions for remaining members
/// - Records cancellation in the audit log
/// - Maintains historical record of the cancelled reservation
/// </remarks>
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
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get reservation with related entities (PRE-2: Active reservation must exist in the system)
            var reservation = await reservationRepository.GetAsync(
                r => r.Id == request.Id,
                r => r.Member,
                r => r.Member.User,
                r => r.Book
            );
            
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found."); // UC023.E1: Reservation Not Found
            
            // Only allow cancellation if the reservation is active (PRE-3: Reservation must be in pending status)
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure($"Only active reservations can be cancelled. Current status: {reservation.Status}"); // UC023.E3: Invalid Reservation Status
            
            // Store details for audit logging
            var memberName = reservation.Member?.User?.FullName ?? $"Member ID: {reservation.MemberId}";
            var bookTitle = reservation.Book?.Title ?? $"Book ID: {reservation.BookId}";
            
            // Update status to cancelled (POST-1: Reservation status is updated to cancelled)
            reservation.Status = ReservationStatus.Cancelled;
            reservation.LastModifiedAt = DateTime.UtcNow;
            
            reservationRepository.Update(reservation);
            
            // Note: Queue positions are implicitly managed by reservation date when retrieving
            // active reservations in order. No explicit queue position adjustment is needed
            // since we use the ReservationDate timestamp for ordering.
            
            // Record cancellation in audit log (POST-5: Cancellation activity is logged for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Reservation",
                EntityId = reservation.Id.ToString(),
                EntityName = $"Reservation for '{bookTitle}'",
                ActionType = AuditActionType.ReservationCancelled,
                Details = $"Reservation for book '{bookTitle}' by member '{memberName}' was cancelled.",
                IsSuccess = true
            });
            
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