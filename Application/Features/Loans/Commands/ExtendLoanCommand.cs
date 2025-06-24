using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record ExtendLoanCommand(ExtendLoanDto ExtendLoanDto) : IRequest<Result>;

public class ExtendLoanCommandHandler : IRequestHandler<ExtendLoanCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ExtendLoanCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ExtendLoanCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            
            // Get loan
            var loan = await loanRepository.GetAsync(
                l => l.Id == request.ExtendLoanDto.LoanId,
                l => l.Member
            );
            
            if (loan == null)
                return Result.Failure($"Loan with ID {request.ExtendLoanDto.LoanId} not found.");
            
            // Check if loan is active
            if (loan.Status != LoanStatus.Active)
                return Result.Failure("Only active loans can be extended.");
            
            // Check if new due date is valid
            if (request.ExtendLoanDto.NewDueDate <= loan.DueDate)
                return Result.Failure("New due date must be after the current due date.");
            
            // Check if new due date is too far in the future (e.g., max 30 days from now)
            var maxExtensionDate = DateTime.Now.AddDays(30);
            if (request.ExtendLoanDto.NewDueDate > maxExtensionDate)
                return Result.Failure($"New due date cannot be more than 30 days from now ({maxExtensionDate:yyyy-MM-dd}).");
            
            // Check if member is active
            if (loan.Member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure("Loan cannot be extended because member is not active.");
            
            // Update due date
            loan.DueDate = request.ExtendLoanDto.NewDueDate;
            
            loanRepository.Update(loan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to extend loan: {ex.Message}");
        }
    }
}