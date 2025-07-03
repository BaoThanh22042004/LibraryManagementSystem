using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

/// <summary>
/// Command to fulfill a reservation when a book becomes available (UC024 - Fulfill Reservation).
/// </summary>
/// <remarks>
/// This implementation follows UC024 specifications:
/// - Validates reservation exists and is in active status
/// - Ensures the book copy is available and matches the reserved book
/// - Updates reservation status to fulfilled
/// - Sets aside the specific copy for the member
/// - Updates book copy status to reserved
/// - Establishes pickup deadline (system configuration determines timeframe)
/// - Records fulfillment in the audit log
/// </remarks>
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
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get reservation with book details (PRE-2: Active reservation must exist in the system)
            var reservation = await reservationRepository.GetAsync(
                r => r.Id == request.Id,
                r => r.Book,
                r => r.Member,
                r => r.Member.User
            );
            
            if (reservation == null)
                return Result.Failure($"Reservation with ID {request.Id} not found."); // UC024.E1: No Active Reservations
            
            // Only allow fulfillment if the reservation is active
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure($"Only active reservations can be fulfilled. Current status: {reservation.Status}");
            
            // Get book copy (PRE-3: Book copy must be available for the reserved book)
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.BookCopyId,
                bc => bc.Book
            );
            
            if (bookCopy == null)
                return Result.Failure($"Book copy with ID {request.BookCopyId} not found.");
            
            // Check if book copy is for the correct book
            if (bookCopy.BookId != reservation.BookId)
                return Result.Failure($"Book copy with ID {request.BookCopyId} is not for the reserved book.");
            
            // Check if book copy is available
            if (bookCopy.Status != CopyStatus.Available)
                return Result.Failure($"Book copy is not available. Current status: {bookCopy.Status}."); // UC024.E2: Copy Not Available
            
            // Store details for notification and audit logging
            var memberName = reservation.Member?.User?.FullName ?? $"Member ID: {reservation.MemberId}";
            var memberEmail = reservation.Member?.User?.Email;
            var bookTitle = reservation.Book?.Title ?? $"Book ID: {reservation.BookId}";
            
            // Check if member has valid contact information (PRE-4: Member must have current contact information)
            if (string.IsNullOrEmpty(memberEmail))
                return Result.Failure($"Member does not have valid contact information for notifications."); // UC024.E3: Member Contact Missing
            
            // Update reservation status to fulfilled (POST-1: Reservation status is updated to fulfilled)
            reservation.Status = ReservationStatus.Fulfilled;
            reservation.BookCopyId = bookCopy.Id;
            reservation.LastModifiedAt = DateTime.UtcNow;
            
            // Update book copy status to reserved (POST-2: Book copy is held specifically for the member)
            bookCopy.Status = CopyStatus.Reserved;
            bookCopy.LastModifiedAt = DateTime.UtcNow;
            
            reservationRepository.Update(reservation);
            bookCopyRepository.Update(bookCopy);
            
            // Create pickup deadline (POST-4: Pickup deadline is established)
            var pickupDeadline = DateTime.UtcNow.AddHours(72); // 72 hours (3 days) pickup window
            
            // Record fulfillment in audit log (POST-5: Fulfillment activity is logged for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Reservation",
                EntityId = reservation.Id.ToString(),
                EntityName = $"Reservation Fulfillment for '{bookTitle}'",
                ActionType = AuditActionType.ReservationFulfilled,
                Details = $"Reservation for book '{bookTitle}' fulfilled for member '{memberName}'. " +
                          $"Book copy #{bookCopy.CopyNumber} reserved until {pickupDeadline:yyyy-MM-dd HH:mm}.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            // Note: In a real system, we would send notification here (POST-3: Member is notified of book availability)
            // This would typically be handled by a notification service or event system
            // Example pseudocode:
            // await _notificationService.SendReservationFulfillmentNotification(
            //     memberEmail,
            //     memberName,
            //     bookTitle,
            //     bookCopy.CopyNumber,
            //     pickupDeadline);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to fulfill reservation: {ex.Message}"); // UC024.E4: Notification Failure
        }
    }
}