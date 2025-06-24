using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

public record DeleteFineCommand(int Id) : IRequest<Result>;

public class DeleteFineCommandHandler : IRequestHandler<DeleteFineCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteFineCommand request, CancellationToken cancellationToken)
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
            
            // Can only delete pending fines
            if (fine.Status != FineStatus.Pending)
                return Result.Failure("Only pending fines can be deleted.");
            
            // Update member's outstanding fines
            var member = fine.Member;
            member.OutstandingFines -= fine.Amount;
            memberRepository.Update(member);
            
            // Delete fine
            fineRepository.Delete(fine);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to delete fine: {ex.Message}");
        }
    }
}