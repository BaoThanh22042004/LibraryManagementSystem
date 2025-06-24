using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record UpdateBookCopyStatusCommand(int Id, CopyStatus Status) : IRequest<Result>;

public class UpdateBookCopyStatusCommandHandler : IRequestHandler<UpdateBookCopyStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookCopyStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateBookCopyStatusCommand request, CancellationToken cancellationToken)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        // Get book copy
        var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.Id);
        
        if (bookCopy == null)
            return Result.Failure($"Book copy with ID {request.Id} not found.");
        
        // If marking as Available, check if there are active loans for this copy
        if (request.Status == CopyStatus.Available)
        {
            var activeLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == request.Id && l.Status == LoanStatus.Active
            );
            
            if (activeLoans)
                return Result.Failure($"Cannot mark book copy as Available while it has active loans.");
        }
        
        // If marking as Reserved, check if there are active reservations for this book
        if (request.Status == CopyStatus.Reserved)
        {
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            var bookId = bookCopy.BookId;
            
            var activeReservations = await reservationRepository.ExistsAsync(
                r => r.BookId == bookId && r.Status == ReservationStatus.Active
            );
            
            if (!activeReservations)
                return Result.Failure($"Cannot mark book copy as Reserved without active reservations for this book.");
        }
        
        // Update status
        bookCopy.Status = request.Status;
        
        bookCopyRepository.Update(bookCopy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}