using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve detailed user information with role-based access control (UC007)
/// This implements the user information access functionality:
/// - Enforces BR-03 (User Information Access): Users can only view their own information; staff access is role-based
/// - Members can only view their own information
/// - Librarians can view their own and Member information
/// - Admins can view all user information
/// - Retrieves complete user profile including role-specific details
/// </summary>
public record GetUserDetailsQuery(int Id, int CurrentUserId) : IRequest<UserDetailsDto?>;

public class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserDetailsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDetailsDto?> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Get current user to determine access permissions
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            return null;
        }
        
        // Check access permissions based on role (BR-03)
        bool hasPermission = false;
        
        // Self-access is always permitted (UC007 Normal Flow 7.0)
        if (request.Id == request.CurrentUserId)
        {
            hasPermission = true;
        }
        else if (currentUser.Role == UserRole.Admin)
        {
            // Admins can view any user (UC007 Alternative Flow 7.3)
            hasPermission = true;
        }
        else if (currentUser.Role == UserRole.Librarian)
        {
            // Get target user role to determine if librarian can access
            var targetUser = await userRepository.GetAsync(u => u.Id == request.Id);
            if (targetUser != null)
            {
                // Librarians can only view Member information, not other Librarians or Admins (UC007 Alternative Flow 7.2)
                hasPermission = targetUser.Role == UserRole.Member;
            }
        }
        
        if (!hasPermission)
        {
            // Log unauthorized access attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.AccessSensitiveData,
                EntityType = "User",
                EntityId = request.Id.ToString(),
                Details = $"Unauthorized attempt to access user details for user ID {request.Id} by user {request.CurrentUserId}",
                Module = "UserManagement",
                IsSuccess = false,
                ErrorMessage = "Insufficient permissions"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return null;
        }
        
        // Get user with related entities
        var user = await userRepository.GetAsync(
            u => u.Id == request.Id,
            u => u.Member!,
            u => u.Librarian!);
            
        if (user == null)
        {
            return null;
        }
        
        // Log successful sensitive data access (BR-22)
        await auditLogRepository.AddAsync(new AuditLog
        {
            UserId = request.CurrentUserId,
            ActionType = AuditActionType.AccessSensitiveData,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            EntityName = user.FullName,
            Details = $"User details accessed for {user.Email} by user {request.CurrentUserId}",
            Module = "UserManagement",
            IsSuccess = true
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Map to DTO
        var userDetailsDto = _mapper.Map<UserDetailsDto>(user);
        
        // Map related entities if they exist
        if (user.Member != null)
        {
            userDetailsDto.Member = _mapper.Map<MemberDto>(user.Member);
        }
        
        if (user.Librarian != null)
        {
            userDetailsDto.Librarian = _mapper.Map<LibrarianDto>(user.Librarian);
        }
        
        return userDetailsDto;
    }
}