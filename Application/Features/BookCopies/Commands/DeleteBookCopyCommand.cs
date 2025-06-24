using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record DeleteBookCopyCommand(int Id) : IRequest<Result>;

public class DeleteBookCopyCommandHandler : IRequestHandler<DeleteBookCopyCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCopyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBookCopyCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get book copy with related entities
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.Id,
                bc => bc.Loans,
                bc => bc.Reservations
            );
            
            if (bookCopy == null)
                return Result.Failure($"Book copy with ID {request.Id} not found.");
            
            // Check for active loans
            var hasActiveLoans = bookCopy.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoans)
                return Result.Failure("Cannot delete book copy with active loans. Please return the book first.");
            
            // Check for active reservations
            var hasActiveReservations = bookCopy.Reservations.Any(r => r.Status == ReservationStatus.Active);
            if (hasActiveReservations)
                return Result.Failure("Cannot delete book copy with active reservations. Please cancel the reservations first.");
            
            // Delete book copy
            bookCopyRepository.Delete(bookCopy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"An error occurred while deleting the book copy: {ex.Message}");
        }
    }
}