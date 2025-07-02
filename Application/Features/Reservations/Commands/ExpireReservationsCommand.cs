using Application.Common;
using Application.DTOs;
using Application.Features.Notifications.Commands;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record ExpireReservationsCommand : IRequest<Result<int>>;

public class ExpireReservationsCommandHandler : IRequestHandler<ExpireReservationsCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public ExpireReservationsCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result<int>> Handle(ExpireReservationsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Find fulfilled reservations that have exceeded the pickup window (3 days)
            var expirationDate = DateTime.Now.AddDays(-3);
            var expiredReservations = await reservationRepository.ListAsync(
                predicate: r => r.Status == ReservationStatus.Fulfilled && r.ReservationDate <= expirationDate,
                orderBy: q => q.OrderBy(r => r.Id),
                asNoTracking: false,
                includes: [r => r.Member.User,
                r => r.Book]
            );
            
            if (expiredReservations.Count == 0)
                return Result.Success(0);
            
            int expiredCount = 0;
            
            foreach (var reservation in expiredReservations)
            {
                // Change status to expired
                reservation.Status = ReservationStatus.Expired;
                reservationRepository.Update(reservation);
                
                // If book copy was assigned, make it available again
                if (reservation.BookCopyId.HasValue)
                {
                    var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
                    var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == reservation.BookCopyId.Value);
                    
                    if (bookCopy != null && bookCopy.Status == CopyStatus.Reserved)
                    {
                        bookCopy.Status = CopyStatus.Available;
                        bookCopyRepository.Update(bookCopy);
                    }
                }
                
                // Notify member that reservation has expired
                if (reservation.Member?.User != null)
                {
                    var bookTitle = reservation.Book?.Title ?? "Unknown Book";
                    
                    var createNotificationDto = new CreateNotificationDto
                    {
                        UserId = reservation.Member.User.Id,
                        Type = NotificationType.SystemMaintenance, // Using SystemMaintenance as closest available type
                        Subject = $"Reservation Expired: {bookTitle}",
                        Message = $"Your reservation for '{bookTitle}' has expired because it was not picked up within 3 days of availability."
                    };
                    
                    await _mediator.Send(new CreateNotificationCommand(createNotificationDto), cancellationToken);
                }
                
                expiredCount++;
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(expiredCount);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to process expired reservations: {ex.Message}");
        }
    }
}