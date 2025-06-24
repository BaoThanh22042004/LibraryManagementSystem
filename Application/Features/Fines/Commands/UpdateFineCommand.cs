using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Fines.Commands;

public record UpdateFineCommand(int Id, UpdateFineDto FineDto) : IRequest<Result>;

public class UpdateFineCommandHandler : IRequestHandler<UpdateFineCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            
            // Get fine
            var fine = await fineRepository.GetAsync(f => f.Id == request.Id);
            
            if (fine == null)
                return Result.Failure($"Fine with ID {request.Id} not found.");
            
            // Update only allowed fields
            fine.Status = request.FineDto.Status;
            fine.Description = request.FineDto.Description;
            
            fineRepository.Update(fine);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update fine: {ex.Message}");
        }
    }
}