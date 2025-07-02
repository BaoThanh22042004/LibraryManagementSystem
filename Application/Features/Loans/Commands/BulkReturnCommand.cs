using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record BulkReturnCommand(BulkReturnDto BulkReturnDto) : IRequest<Result<List<(int LoanId, bool Success, string? ErrorMessage)>>>;

public class BulkReturnCommandHandler : IRequestHandler<BulkReturnCommand, Result<List<(int LoanId, bool Success, string? ErrorMessage)>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public BulkReturnCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result<List<(int LoanId, bool Success, string? ErrorMessage)>>> Handle(BulkReturnCommand request, CancellationToken cancellationToken)
    {
        if (request.BulkReturnDto.LoanIds.Count == 0)
        {
            return Result.Failure<List<(int, bool, string?)>>("No loans specified for return.");
        }

        var results = new List<(int LoanId, bool Success, string? ErrorMessage)>();
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            
            foreach (var loanId in request.BulkReturnDto.LoanIds)
            {
                try
                {
                    // Get loan with related entities
                    var loan = await loanRepository.GetAsync(
                        l => l.Id == loanId,
                        l => l.BookCopy,
                        l => l.Member
                    );
                    
                    if (loan == null)
                    {
                        results.Add((loanId, false, $"Loan with ID {loanId} not found."));
                        continue;
                    }
                    
                    // Check if loan is active or overdue
                    if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                    {
                        results.Add((loanId, false, $"Loan with ID {loanId} cannot be returned. Current status: {loan.Status}"));
                        continue;
                    }
                    
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
                            Type = FineType.Overdue,
                            MemberId = loan.MemberId,
                            LoanId = loan.Id
                        };
                        
                        await fineRepository.AddAsync(fine);
                        
                        // Update member's outstanding fines
                        loan.Member.OutstandingFines += fineAmount;
                        memberRepository.Update(loan.Member);
                    }
                    
                    loanRepository.Update(loan);
                    results.Add((loanId, true, null));
                }
                catch (Exception ex)
                {
                    results.Add((loanId, false, $"Error processing return: {ex.Message}"));
                }
            }
            
            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            // Check if any successful returns
            if (results.All(r => !r.Success))
            {
                return Result.Failure<List<(int, bool, string?)>>("Failed to return any books.");
            }
            
            return Result.Success(results);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<List<(int, bool, string?)>>($"Failed to process bulk return: {ex.Message}");
        }
    }
}