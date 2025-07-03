using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to send notifications to users when their reserved books become available.
/// Implements UC035: Send Reservation Available Notifications
/// </summary>
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
            
            // Get all active reservations ordered by reservation date (first come, first served)   
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
            var processedBooks = new HashSet<int>(); // Track books we've already processed
            List<string> errors = new();
            
            // Group reservations by book to handle first-in-queue notification
            var reservationsByBook = activeReservations
                .GroupBy(r => r.BookId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.ReservationDate).ToList());
            
            // Process reservations
            foreach (var bookId in reservationsByBook.Keys)
            {
                // Skip if we've already processed this book
                if (processedBooks.Contains(bookId))
                    continue;
                
                // Check if there are available copies of the book
                var availableCopies = await bookCopyRepository.ListAsync(
                    predicate: bc => bc.BookId == bookId && bc.Status == CopyStatus.Available,      
                    orderBy: null,
                    true
                );
                
                if (availableCopies.Count > 0)
                {
                    // Get the first reservation in the queue for this book
                    var firstReservation = reservationsByBook[bookId].FirstOrDefault();
                    
                    if (firstReservation?.Member?.User == null)
                    {
                        errors.Add($"Book ID {bookId}: Member information missing for reservation");
                        continue;
                    }
                    
                    var userId = firstReservation.Member.User.Id;
                    var bookTitle = firstReservation.Book?.Title ?? "Unknown Book";
                    
                    // Calculate pickup deadline (3 days from now)
                    var pickupDeadline = DateTime.UtcNow.AddDays(3).ToString("MMMM d, yyyy");       
                    
                    var createNotificationDto = new CreateNotificationDto
                    {
                        UserId = userId,
                        Type = NotificationType.ReservationAvailable,
                        Subject = $"Book Available: {bookTitle}",
                        Message = $"Good news! A copy of '{bookTitle}' is now available for you to borrow. Please visit the library by {pickupDeadline} to check out the book. If not collected by then, the reservation will expire and the book will be offered to the next person in the queue."
                    };
                    
                    var result = await _mediator.Send(new CreateNotificationCommand(createNotificationDto), cancellationToken);
                    
                    if (result.IsSuccess)
                    {
                        // Update the notification to 'Sent' status
                        await _mediator.Send(new UpdateNotificationCommand(result.Value, new UpdateNotificationDto { Status = NotificationStatus.Sent }), cancellationToken);
                        successCount++;
                        
                        // Mark this book as processed
                        processedBooks.Add(bookId);
                    }
                    else
                    {
                        errors.Add($"Book ID {bookId}: {result.Error}");
                    }
                }
            }
            
            await _unitOfWork.CommitTransactionAsync();
            
            string resultMessage = $"Successfully sent {successCount} reservation availability notifications.";
            if (errors.Count > 0)
            {
                resultMessage += $" Encountered {errors.Count} errors: {string.Join("; ", errors)}";
            }
            
            return Result.Success(resultMessage);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to send reservation availability notifications: {ex.Message}");
        }
    }
}