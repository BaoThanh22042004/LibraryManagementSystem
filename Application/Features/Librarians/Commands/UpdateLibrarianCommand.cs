using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Librarians.Commands;

public record UpdateLibrarianCommand(int Id, UpdateLibrarianDto LibrarianDto) : IRequest<Result>;

public class UpdateLibrarianCommandHandler : IRequestHandler<UpdateLibrarianCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLibrarianCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateLibrarianCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var librarianRepository = _unitOfWork.Repository<Librarian>();
            
            // Get librarian by ID
            var librarian = await librarianRepository.GetAsync(l => l.Id == request.Id);
            
            if (librarian == null)
            {
                return Result.Failure($"Librarian with ID {request.Id} not found.");
            }
            
            // Check if new employee ID is unique (if it changed)
            if (librarian.EmployeeId != request.LibrarianDto.EmployeeId)
            {
                var employeeIdExists = await librarianRepository.ExistsAsync(
                    l => l.EmployeeId == request.LibrarianDto.EmployeeId && l.Id != request.Id
                );
                
                if (employeeIdExists)
                {
                    return Result.Failure($"Librarian with employee ID '{request.LibrarianDto.EmployeeId}' already exists.");
                }
            }
            
            // Update properties
            librarian.EmployeeId = request.LibrarianDto.EmployeeId;
            
            // Update HireDate if provided
            if (request.LibrarianDto.HireDate.HasValue)
            {
                librarian.HireDate = request.LibrarianDto.HireDate.Value;
            }
            
            librarianRepository.Update(librarian);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to update librarian: {ex.Message}");
        }
    }
}