using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reservations.Commands;

public record CreateReservationCommand(CreateReservationDto ReservationDto) : IRequest<Result<int>>;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateReservationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
            
            // Validate member exists and is active
            var member = await memberRepository.GetAsync(m => m.Id == request.ReservationDto.MemberId);
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.ReservationDto.MemberId} not found.");
            
            if (member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure<int>($"Member with ID {request.ReservationDto.MemberId} is not active. Current status: {member.MembershipStatus}.");
            
            // Validate book exists
            var book = await bookRepository.GetAsync(b => b.Id == request.ReservationDto.BookId);
            if (book == null)
                return Result.Failure<int>($"Book with ID {request.ReservationDto.BookId} not found.");
            
            // Check if member already has an active reservation for this book
            var existingReservation = await reservationRepository.ExistsAsync(r => 
                r.MemberId == request.ReservationDto.MemberId && 
                r.BookId == request.ReservationDto.BookId && 
                r.Status == ReservationStatus.Active);
            
            if (existingReservation)
                return Result.Failure<int>($"Member already has an active reservation for this book.");
            
            // Check if there are available copies of the book
            var availableCopies = await bookCopyRepository.ExistsAsync(bc => 
                bc.BookId == request.ReservationDto.BookId && 
                bc.Status == CopyStatus.Available);
            
            // If there are available copies, we might not need a reservation
            if (availableCopies)
                return Result.Failure<int>($"There are available copies of this book. A reservation is not needed.");
            
            // Create reservation
            var reservation = new Reservation
            {
                MemberId = request.ReservationDto.MemberId,
                BookId = request.ReservationDto.BookId,
                ReservationDate = DateTime.Now,
                Status = ReservationStatus.Active
            };
            
            await reservationRepository.AddAsync(reservation);
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