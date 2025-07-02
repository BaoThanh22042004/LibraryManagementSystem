using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to handle user authentication (UC002)
/// This implements the login functionality:
/// - Validates user credentials
/// - Generates authentication token
/// - Maintains audit trail for security monitoring
/// </summary>
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
        var userRepository = _unitOfWork.Repository<User>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Find user by email
        var user = await userRepository.GetAsync(u => u.Email.ToLower() == request.LoginDto.Email.ToLower());
        
        // Check if user exists (UC002 Exception 2.0.E1)
        if (user == null)
        {
            // Log failed login attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = null, // No user found
                ActionType = AuditActionType.LoginFailed,
                EntityType = "User",
                EntityId = null,
                EntityName = null,
                Details = $"Failed login attempt for email: {request.LoginDto.Email} - user not found",
                Module = "Authentication",
                IsSuccess = false,
                ErrorMessage = "Invalid email or password"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new LoginResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password." // Generic message for security
            };
        }
        
        // Verify password (UC002 Exception 2.0.E1)
        if (!PasswordHasher.VerifyPassword(request.LoginDto.Password, user.PasswordHash))
        {
            // Log failed login attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                ActionType = AuditActionType.LoginFailed,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Failed login attempt for user: {user.Email} - incorrect password",
                Module = "Authentication",
                IsSuccess = false,
                ErrorMessage = "Invalid password"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new LoginResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password." // Generic message for security
            };
        }
        
        // Generate token
        var token = _tokenGenerator.GenerateToken(user);
        
        // Log successful login (BR-22)
        await auditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            ActionType = AuditActionType.Login,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            EntityName = user.FullName,
            Details = $"Successful login for user: {user.Email}",
            Module = "Authentication",
            IsSuccess = true
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
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