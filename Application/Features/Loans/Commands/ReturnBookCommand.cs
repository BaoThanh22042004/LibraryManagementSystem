using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record ReturnBookCommand(int LoanId) : IRequest<Result>;

public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReturnBookCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var fineRepository = _unitOfWork.Repository<Fine>();
            
            // Get loan
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.LoanId,
                l => l.BookCopy,
                l => l.Member
            );
            
            if (loan == null)
                return Result.Failure($"Loan with ID {request.LoanId} not found.");
            
            // Check if loan is active or overdue
            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return Result.Failure("Only active or overdue loans can be returned.");
            
            // Set return date and update status
            loan.ReturnDate = DateTime.Now;
            loan.Status = LoanStatus.Returned;
            
            // Update book copy status
            var bookCopy = loan.BookCopy;
            bookCopy.Status = CopyStatus.Available;
            bookCopyRepository.Update(bookCopy);
            
            // Check if book is returned late and create fine if needed
            if (loan.DueDate < loan.ReturnDate)
            {
                // Calculate days overdue
                int daysOverdue = (int)(loan.ReturnDate.Value - loan.DueDate).TotalDays;
                
                // Calculate fine amount (e.g., $0.50 per day)
                decimal fineAmount = daysOverdue * 0.50m;
                
                // Create fine
                var fine = new Fine
                {
                    Amount = fineAmount,
                    Description = $"Late return fine for loan #{loan.Id}. {daysOverdue} days overdue.",
                    FineDate = DateTime.Now,
                    Status = FineStatus.Pending,
                    Type = FineType.Overdue,  // Using the correct FineType
                    MemberId = loan.MemberId,
                    LoanId = loan.Id
                };
                
                await fineRepository.AddAsync(fine);
                
                // Update member's outstanding fines
                loan.Member.OutstandingFines += fineAmount;
                _unitOfWork.Repository<Member>().Update(loan.Member);
            }
            
            loanRepository.Update(loan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to return book: {ex.Message}");
        }
    }
}