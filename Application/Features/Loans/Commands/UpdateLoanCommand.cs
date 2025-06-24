using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record UpdateLoanCommand(int Id, UpdateLoanDto LoanDto) : IRequest<Result>;

public class UpdateLoanCommandHandler : IRequestHandler<UpdateLoanCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateLoanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateLoanCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            
            // Get loan
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.Id,
                l => l.BookCopy
            );
            
            if (loan == null)
                return Result.Failure($"Loan with ID {request.Id} not found.");
            
            // Handle return date update if provided
            if (request.LoanDto.ReturnDate.HasValue)
            {
                if (request.LoanDto.ReturnDate.Value < loan.LoanDate)
                    return Result.Failure("Return date cannot be earlier than loan date.");
                
                loan.ReturnDate = request.LoanDto.ReturnDate;
            }
            
            // Update status
            var previousStatus = loan.Status;
            loan.Status = request.LoanDto.Status;
            
            // Handle status-specific actions
            if (loan.Status == LoanStatus.Returned && previousStatus != LoanStatus.Returned)
            {
                // Set return date if not already set
                if (!loan.ReturnDate.HasValue)
                    loan.ReturnDate = DateTime.Now;
                
                // Update book copy status to Available
                var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == loan.BookCopyId);
                if (bookCopy != null)
                {
                    bookCopy.Status = CopyStatus.Available;
                    bookCopyRepository.Update(bookCopy);
                }
            }
            else if (loan.Status == LoanStatus.Lost && previousStatus != LoanStatus.Lost)
            {
                // Update book copy status to Lost
                var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == loan.BookCopyId);
                if (bookCopy != null)
                {
                    bookCopy.Status = CopyStatus.Lost;
                    bookCopyRepository.Update(bookCopy);
                }
                
                // Handle fines for lost book - could be implemented in a separate service
            }
            
            loanRepository.Update(loan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update loan: {ex.Message}");
        }
    }
}