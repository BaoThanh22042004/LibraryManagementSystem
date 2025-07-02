using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

/// <summary>
/// Command to process a fine payment.
/// </summary>
public record PayFineCommand(int Id) : IRequest<Result>;

public class PayFineCommandHandler : IRequestHandler<PayFineCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public PayFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PayFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            
            // Get fine
            var fine = await fineRepository.GetAsync(
                f => f.Id == request.Id,
                f => f.Member
            );
            
            if (fine == null)
                return Result.Failure($"Fine with ID {request.Id} not found.");
            
            // Check if the fine is already paid or waived
            if (fine.Status != FineStatus.Pending)
                return Result.Failure("Only pending fines can be paid.");
            
            // Update fine status
            fine.Status = FineStatus.Paid;
            fine.LastModifiedAt = DateTime.Now;
            fineRepository.Update(fine);
            
            // Update member's outstanding fines
            var member = fine.Member;
            member.OutstandingFines -= fine.Amount;
            memberRepository.Update(member);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to pay fine: {ex.Message}");
        }
    }
}