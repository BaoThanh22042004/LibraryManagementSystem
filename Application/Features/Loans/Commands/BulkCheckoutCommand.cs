using Application.Common;
using Application.DTOs;
using Application.Features.Loans.Queries;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record BulkCheckoutCommand(BulkCheckoutDto BulkCheckoutDto) : IRequest<Result<List<int>>>;

public class BulkCheckoutCommandHandler : IRequestHandler<BulkCheckoutCommand, Result<List<int>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private const int MaxAllowedLoans = 5;
    private const int DefaultLoanDays = 14;

    public BulkCheckoutCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result<List<int>>> Handle(BulkCheckoutCommand request, CancellationToken cancellationToken)
    {
        if (request.BulkCheckoutDto.BookCopyIds.Count == 0)
        {
            return Result.Failure<List<int>>("No book copies specified for checkout.");
        }

        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            
            // Check eligibility first
            var eligibilityResult = await _mediator.Send(new CheckLoanEligibilityQuery(request.BulkCheckoutDto.MemberId), cancellationToken);
            
            if (eligibilityResult.IsFailure)
                return Result.Failure<List<int>>(eligibilityResult.Error);
                
            var eligibility = eligibilityResult.Value;
            
            if (!eligibility.IsEligible)
                return Result.Failure<List<int>>($"Member is not eligible to borrow books: {string.Join("; ", eligibility.Reasons)}");
            
            // Check if trying to borrow too many books
            if (request.BulkCheckoutDto.BookCopyIds.Count > eligibility.AvailableLoanSlots)
                return Result.Failure<List<int>>($"Cannot checkout {request.BulkCheckoutDto.BookCopyIds.Count} books. Member has only {eligibility.AvailableLoanSlots} available loan slots.");
            
            // Get member
            var member = await memberRepository.GetAsync(m => m.Id == request.BulkCheckoutDto.MemberId);
            if (member == null)
                return Result.Failure<List<int>>($"Member with ID {request.BulkCheckoutDto.MemberId} not found.");
            
            // Validate all book copies exist and are available
            var createdLoanIds = new List<int>();
            var unavailableBooks = new List<string>();
            
            foreach (var bookCopyId in request.BulkCheckoutDto.BookCopyIds)
            {
                // Get book copy with book details
                var bookCopy = await bookCopyRepository.GetAsync(
                    bc => bc.Id == bookCopyId,
                    bc => bc.Book
                );
                
                if (bookCopy == null)
                {
                    unavailableBooks.Add($"Book copy with ID {bookCopyId} not found.");
                    continue;
                }
                
                if (bookCopy.Status != CopyStatus.Available)
                {
                    unavailableBooks.Add($"Book copy '{bookCopy.Book.Title}' (Copy #{bookCopy.CopyNumber}) is not available. Current status: {bookCopy.Status}.");
                    continue;
                }
                
                // Calculate due date
                DateTime loanDate = DateTime.Now;
                DateTime dueDate = request.BulkCheckoutDto.CustomDueDate ?? loanDate.AddDays(DefaultLoanDays);
                
                // Create loan
                var loan = new Loan
                {
                    MemberId = request.BulkCheckoutDto.MemberId,
                    BookCopyId = bookCopyId,
                    LoanDate = loanDate,
                    DueDate = dueDate,
                    Status = LoanStatus.Active
                };
                
                await loanRepository.AddAsync(loan);
                
                // Update book copy status
                bookCopy.Status = CopyStatus.Borrowed;
                bookCopyRepository.Update(bookCopy);
                
                // Save changes to get the loan ID
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                createdLoanIds.Add(loan.Id);
            }
            
            // If any unavailable books, rollback and return error
            if (unavailableBooks.Count > 0)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure<List<int>>($"Could not complete checkout: {string.Join("; ", unavailableBooks)}");
            }
            
            await _unitOfWork.CommitTransactionAsync();
            return Result.Success(createdLoanIds);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<List<int>>($"Failed to process bulk checkout: {ex.Message}");
        }
    }
}