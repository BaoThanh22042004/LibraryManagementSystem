using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record DeleteBookCopyCommand(int Id) : IRequest<Result<bool>>;

public class DeleteBookCopyCommandHandler : IRequestHandler<DeleteBookCopyCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCopyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteBookCopyCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get book copy
            var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.Id);
            
            if (bookCopy == null)
                return Result.Failure<bool>($"Book copy with ID {request.Id} not found.");
            
            // Check for active loans
            var hasActiveLoans = await loanRepository.ExistsAsync(
                l => l.BookCopyId == request.Id && 
                    (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            if (hasActiveLoans)
                return Result.Failure<bool>("Cannot delete book copy with active loans. Please return the book first.");
            
            // Check for active reservations
            var hasActiveReservations = await reservationRepository.ExistsAsync(
                r => r.BookId == bookCopy.BookId && 
                     r.BookCopyId == request.Id && 
                     r.Status == ReservationStatus.Active
            );
            
            if (hasActiveReservations)
                return Result.Failure<bool>("Cannot delete book copy with active reservations. Please cancel the reservations first.");
            
            // Delete book copy
            bookCopyRepository.Delete(bookCopy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"An error occurred while deleting the book copy: {ex.Message}");
        }
    }
}