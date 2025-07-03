using Application.Common;
using Application.DTOs;
using Application.Features.Reservations.Queries;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

/// <summary>
/// Command to create a new reservation for a book (UC022 - Reserve Book).
/// </summary>
/// <remarks>
/// This implementation follows UC022 specifications:
/// - Validates member exists with active membership
/// - Verifies book exists in the catalog
/// - Checks that all copies of the book are currently on loan
/// - Ensures member doesn't have an existing reservation for the same book
/// - Creates reservation record with active status
/// - Assigns queue position based on reservation date
/// - Records reservation creation in the audit log
/// - Enforces member reservation limits
/// </remarks>
public record CreateReservationCommand(CreateReservationDto ReservationDto) : IRequest<Result<int>>;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public CreateReservationCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result<int>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var bookRepository = _unitOfWork.Repository<Book>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Validate member exists and is active (PRE-1, PRE-2: Member must have active membership status)
            var member = await memberRepository.GetAsync(
                m => m.Id == request.ReservationDto.MemberId,
                m => m.User
            );
            
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.ReservationDto.MemberId} not found."); // UC022.E1: Member Not Found
            
            if (member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure<int>($"Member with ID {request.ReservationDto.MemberId} is not active. Current status: {member.MembershipStatus}."); // UC022.E2: Inactive Membership
            
            // Validate book exists (PRE-3: Book must exist in the library catalog)
            var book = await bookRepository.GetAsync(b => b.Id == request.ReservationDto.BookId);
            if (book == null)
                return Result.Failure<int>($"Book with ID {request.ReservationDto.BookId} not found."); // UC022.E3: Book Not Found
            
            // Check if member already has an active reservation for this book (PRE-5: Member must not have existing reservation)
            var existingReservation = await reservationRepository.ExistsAsync(r => 
                r.MemberId == request.ReservationDto.MemberId && 
                r.BookId == request.ReservationDto.BookId && 
                r.Status == ReservationStatus.Active);
            
            if (existingReservation)
                return Result.Failure<int>($"Member already has an active reservation for this book."); // UC022.E4: Duplicate Reservation
            
            // Check if there are available copies of the book (PRE-4: All copies must be on loan)
            var availableCopies = await bookCopyRepository.ExistsAsync(bc => 
                bc.BookId == request.ReservationDto.BookId && 
                bc.Status == CopyStatus.Available);
            
            // If there are available copies, we might not need a reservation
            if (availableCopies)
                return Result.Failure<int>($"There are available copies of this book. A reservation is not needed."); // UC022.E5: Available Copy Found
            
            // Check if member has reached their reservation limit
            var activeReservationCount = await _mediator.Send(new GetReservationCountForMemberQuery(request.ReservationDto.MemberId), cancellationToken);
            if (activeReservationCount >= 3) // Maximum of 3 active reservations per member
                return Result.Failure<int>($"Member has reached the maximum number of active reservations (3).");
            
            // Create reservation (POST-1, POST-2: Reservation record is created, Member placed in queue)
            var reservation = new Reservation
            {
                MemberId = request.ReservationDto.MemberId,
                BookId = request.ReservationDto.BookId,
                ReservationDate = DateTime.UtcNow,
                Status = ReservationStatus.Active
            };
            
            await reservationRepository.AddAsync(reservation);
            
            // Record reservation creation in audit log (POST-5: Reservation activity is logged)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Reservation",
                EntityId = reservation.Id.ToString(),
                EntityName = $"Reservation for '{book.Title}'",
                ActionType = AuditActionType.ReservationCreated,
                Details = $"Reservation created for book '{book.Title}' by member '{member.User?.FullName ?? $"ID: {member.Id}"}'. Queue position based on timestamp.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(reservation.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create reservation: {ex.Message}");
        }
    }
}