using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using MediatR;

namespace Application.Features.Users.Commands;

public record LoginCommand(LoginDto LoginDto) : IRequest<LoginResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ITokenGenerator tokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        
        // Find user by email
        var user = await userRepository.GetAsync(u => u.Email.ToLower() == request.LoginDto.Email.ToLower());
        
        // Check if user exists
        if (user == null)
        {
            return new LoginResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }
        
        // Verify password
        if (!PasswordHasher.VerifyPassword(request.LoginDto.Password, user.PasswordHash))
        {
            return new LoginResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }
        
        // Generate token
        var token = _tokenGenerator.GenerateToken(user);
        
        // Return success response
        return new LoginResponseDto
        {
            User = _mapper.Map<UserDto>(user),
            Token = token,
            IsSuccess = true,
            Message = "Login successful."
        };
    }
}