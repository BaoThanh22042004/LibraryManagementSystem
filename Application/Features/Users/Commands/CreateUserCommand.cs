using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

public record CreateUserCommand(CreateUserDto UserDto) : IRequest<Result<int>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Check if email already exists
        var emailExists = await userRepository.ExistsAsync(u => u.Email.ToLower() == request.UserDto.Email.ToLower());
        if (emailExists)
        {
            return Result.Failure<int>($"User with email '{request.UserDto.Email}' already exists.");
        }

        // Create new user
        var user = _mapper.Map<User>(request.UserDto);
        
        // Hash the password
        user.PasswordHash = PasswordHasher.HashPassword(request.UserDto.Password);
        
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(user.Id);
    }
}