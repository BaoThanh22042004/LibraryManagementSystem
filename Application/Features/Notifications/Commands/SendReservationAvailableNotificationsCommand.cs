using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

public record SendReservationAvailableNotificationsCommand : IRequest<Result>;

public class SendReservationAvailableNotificationsCommandHandler : IRequestHandler<SendReservationAvailableNotificationsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public SendReservationAvailableNotificationsCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result> Handle(SendReservationAvailableNotificationsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            
            // Get all active reservations
            var activeReservations = await reservationRepository.ListAsync(
                predicate: r => r.Status == ReservationStatus.Active,
                orderBy: q => q.OrderBy(r => r.ReservationDate),
                true,
                r => r.Member.User,
                r => r.Book
            );
            
            if (activeReservations.Count == 0)
                return Result.Success("No active reservations found.");
            
            int successCount = 0;
            
            // Check each reservation to see if a copy is available
            foreach (var reservation in activeReservations)
            {
                // Skip if user is null
                if (reservation.Member?.User == null)
                    continue;
                
                // Check if there are available copies of the book
                var availableCopies = await bookCopyRepository.ListAsync(
                    predicate: bc => bc.BookId == reservation.BookId && bc.Status == CopyStatus.Available,
                    orderBy: null,
                    true
                );
                
                if (availableCopies.Count > 0)
                {
                    var userId = reservation.Member.User.Id;
                    var bookTitle = reservation.Book?.Title ?? "Unknown Book";
                    
                    var createNotificationDto = new CreateNotificationDto
                    {
                        UserId = userId,
                        Type = NotificationType.ReservationAvailable,
                        Subject = $"Book Available: {bookTitle}",
                        Message = $"Good news! A copy of '{bookTitle}' is now available for you to borrow. Please visit the library within the next 3 days to check out the book."
                    };
                    
                    var result = await _mediator.Send(new CreateNotificationCommand(createNotificationDto), cancellationToken);
                    
                    if (result.IsSuccess)
                    {
                        // Update the notification to 'Sent' status
                        await _mediator.Send(new UpdateNotificationCommand(result.Value, new UpdateNotificationDto { Status = NotificationStatus.Sent }), cancellationToken);
                        successCount++;
                    }
                }
            }
            
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success($"Successfully sent {successCount} reservation available notifications.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to send reservation available notifications: {ex.Message}");
        }
    }
}