using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Librarians.Commands;

public record DeleteLibrarianCommand(int Id) : IRequest<Result>;

public class DeleteLibrarianCommandHandler : IRequestHandler<DeleteLibrarianCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLibrarianCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteLibrarianCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var librarianRepository = _unitOfWork.Repository<Librarian>();
            
            // Get librarian with User relationship
            var librarian = await librarianRepository.GetAsync(
                l => l.Id == request.Id,
                l => l.User
            );
            
            if (librarian == null)
            {
                return Result.Failure($"Librarian with ID {request.Id} not found.");
            }
            
            // Delete the librarian record
            librarianRepository.Delete(librarian);
            
            // Note: We're not changing the user's role here as they might be reassigned
            // If needed, this should be handled by the client application
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to delete librarian: {ex.Message}");
        }
    }
}