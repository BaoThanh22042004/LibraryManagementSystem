using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

public record UpdateUserCommand(int Id, UpdateUserDto UserDto) : IRequest<Result>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Get current user using GetAsync instead of GetByIdAsync
        var user = await userRepository.GetAsync(u => u.Id == request.Id);
        
        if (user == null)
        {
            return Result.Failure($"User with ID {request.Id} not found.");
        }
        
        // Check if email is changed and already exists
        if (user.Email.ToLower() != request.UserDto.Email.ToLower())
        {
            var emailExists = await userRepository.ExistsAsync(
                u => u.Email.ToLower() == request.UserDto.Email.ToLower() && u.Id != request.Id
            );
            
            if (emailExists)
            {
                return Result.Failure($"User with email '{request.UserDto.Email}' already exists.");
            }
        }
        
        // Update user properties
        user.FullName = request.UserDto.FullName;
        user.Email = request.UserDto.Email;
        user.Role = request.UserDto.Role;
        
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}