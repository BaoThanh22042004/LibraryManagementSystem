using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Librarians.Commands;

public record CreateLibrarianCommand(CreateLibrarianDto LibrarianDto) : IRequest<Result<int>>;

public class CreateLibrarianCommandHandler : IRequestHandler<CreateLibrarianCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateLibrarianCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateLibrarianCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var librarianRepository = _unitOfWork.Repository<Librarian>();
            var userRepository = _unitOfWork.Repository<User>();
            
            // Check if employee ID already exists
            var employeeIdExists = await librarianRepository.ExistsAsync(
                l => l.EmployeeId == request.LibrarianDto.EmployeeId
            );
            
            if (employeeIdExists)
            {
                return Result.Failure<int>($"Librarian with employee ID '{request.LibrarianDto.EmployeeId}' already exists.");
            }
            
            // Check if user exists
            var user = await userRepository.GetAsync(u => u.Id == request.LibrarianDto.UserId);
            
            if (user == null)
            {
                return Result.Failure<int>($"User with ID {request.LibrarianDto.UserId} not found.");
            }
            
            // Check if user is already a librarian
            var userIsLibrarian = await librarianRepository.ExistsAsync(l => l.UserId == request.LibrarianDto.UserId);
            
            if (userIsLibrarian)
            {
                return Result.Failure<int>($"User with ID {request.LibrarianDto.UserId} is already a librarian.");
            }
            
            // Update user role if needed
            if (user.Role != UserRole.Librarian)
            {
                user.Role = UserRole.Librarian;
                userRepository.Update(user);
            }
            
            // Create new librarian
            var librarian = _mapper.Map<Librarian>(request.LibrarianDto);
            
            await librarianRepository.AddAsync(librarian);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(librarian.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create librarian: {ex.Message}");
        }
    }
}