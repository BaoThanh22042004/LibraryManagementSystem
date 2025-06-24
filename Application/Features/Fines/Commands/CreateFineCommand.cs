using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

public record CreateFineCommand(CreateFineDto FineDto) : IRequest<Result<int>>;

public class CreateFineCommandHandler : IRequestHandler<CreateFineCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            
            // Check if member exists
            var member = await memberRepository.GetAsync(m => m.Id == request.FineDto.MemberId);
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.FineDto.MemberId} not found.");
            
            // Check if loan exists if loanId is provided
            if (request.FineDto.LoanId.HasValue)
            {
                var loanRepository = _unitOfWork.Repository<Loan>();
                var loan = await loanRepository.GetAsync(l => l.Id == request.FineDto.LoanId.Value);
                if (loan == null)
                    return Result.Failure<int>($"Loan with ID {request.FineDto.LoanId.Value} not found.");
            }
            
            // Create fine
            var fine = new Fine
            {
                MemberId = request.FineDto.MemberId,
                LoanId = request.FineDto.LoanId,
                Type = request.FineDto.Type,
                Amount = request.FineDto.Amount,
                Description = request.FineDto.Description,
                FineDate = DateTime.Now,
                Status = FineStatus.Pending
            };
            
            await fineRepository.AddAsync(fine);
            
            // Update member's outstanding fines
            member.OutstandingFines += fine.Amount;
            memberRepository.Update(member);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(fine.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create fine: {ex.Message}");
        }
    }
}