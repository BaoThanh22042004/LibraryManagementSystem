using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Books.Commands;

public record DeleteBookCommand(int Id) : IRequest<Result>;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var loanRepository = _unitOfWork.Repository<Loan>();
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        // Get book with copies
        var book = await bookRepository.GetAsync(
            b => b.Id == request.Id,
            b => b.Copies
        );
        
        if (book == null)
        {
            return Result.Failure($"Book with ID {request.Id} not found.");
        }
        
        // Check if book has active loans
        if (book.Copies.Any())
        {
            var copyIds = book.Copies.Select(c => c.Id).ToList();
            var hasActiveLoans = await loanRepository.ExistsAsync(
                l => copyIds.Contains(l.BookCopyId) && l.Status == LoanStatus.Active
            );
            
            if (hasActiveLoans)
            {
                return Result.Failure("Cannot delete book with active loans.");
            }
        }
        
        // Check if book has active reservations
        var hasActiveReservations = await reservationRepository.ExistsAsync(
            r => r.BookId == request.Id && r.Status == ReservationStatus.Active
        );
        
        if (hasActiveReservations)
        {
            return Result.Failure("Cannot delete book with active reservations.");
        }
        
        // Delete book
        bookRepository.Delete(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}