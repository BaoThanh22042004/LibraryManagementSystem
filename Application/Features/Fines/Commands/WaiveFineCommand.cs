using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

public record WaiveFineCommand(int Id) : IRequest<Result>;

public class WaiveFineCommandHandler : IRequestHandler<WaiveFineCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public WaiveFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(WaiveFineCommand request, CancellationToken cancellationToken)
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
                return Result.Failure("Only pending fines can be waived.");
            
            // Update fine status
            fine.Status = FineStatus.Waived;
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
            return Result.Failure($"Failed to waive fine: {ex.Message}");
        }
    }
}