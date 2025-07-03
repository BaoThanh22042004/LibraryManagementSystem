using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

/// <summary>
/// Command to process a book return (UC019 - Return Book).
/// </summary>
/// <remarks>
/// This implementation follows UC019 specifications:
/// - Updates loan record with return date and returned status
/// - Updates book copy status to available
/// - Calculates overdue fines if returned after due date
/// - Creates fine record for overdue returns
/// - Updates member's outstanding balance
/// - Records return transaction for audit purposes
/// </remarks>
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
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get loan with related entities (PRE-3: Loan must exist with active or overdue status)
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.LoanId,
                l => l.BookCopy,
                l => l.BookCopy.Book,
                l => l.Member
            );
            
            if (loan == null)
                return Result.Failure($"Loan with ID {request.LoanId} not found."); // UC019.E1: Loan Not Found
            
            // Check if loan is active or overdue
            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return Result.Failure("Only active or overdue loans can be returned."); // UC019.E2: Loan Already Returned
            
            // Store original status for audit and reporting
            var originalStatus = loan.Status;
            
            // Set return date and update status (POST-1: Loan record is updated with return date and returned status)
            loan.ReturnDate = DateTime.Now;
            loan.Status = LoanStatus.Returned;
            
            // Update book copy status (POST-2: Book copy status is updated to available)
            var bookCopy = loan.BookCopy;
            bookCopy.Status = CopyStatus.Available;
            bookCopyRepository.Update(bookCopy);
            
            // POST-4: Calculate overdue fines if applicable (UC019.1: Overdue Return with Fine)
            decimal fineAmount = 0;
            int daysOverdue = 0;
            
            if (loan.DueDate < loan.ReturnDate)
            {
                // Calculate days overdue
                daysOverdue = (int)(loan.ReturnDate.Value - loan.DueDate).TotalDays;
                
                // Calculate fine amount (e.g., $0.50 per day)
                fineAmount = daysOverdue * 0.50m;
                
                // Create fine record
                var fine = new Fine
                {
                    Amount = fineAmount,
                    Description = $"Late return fine for loan #{loan.Id}. {daysOverdue} days overdue.",
                    FineDate = DateTime.Now,
                    Status = FineStatus.Pending,
                    Type = FineType.Overdue,
                    MemberId = loan.MemberId,
                    LoanId = loan.Id
                };
                
                await fineRepository.AddAsync(fine);
                
                // Update member's outstanding fines (POST-7: Member's outstanding fines balance is updated if applicable)
                loan.Member.OutstandingFines += fineAmount;
                _unitOfWork.Repository<Member>().Update(loan.Member);
            }
            
            // Update loan record
            loanRepository.Update(loan);
            
            // Record return in audit log (POST-5: Return transaction is recorded for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Loan",
                EntityId = loan.Id.ToString(),
                EntityName = $"Return of '{loan.BookCopy.Book.Title}'",
                ActionType = AuditActionType.BookReturn,
                Details = daysOverdue > 0
                    ? $"Book returned {daysOverdue} days late. Fine of ${fineAmount:F2} applied."
                    : "Book returned on time.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to return book: {ex.Message}"); // UC019.E3: System Error During Processing
        }
    }
}