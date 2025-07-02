using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

public record AdminUpdateUserCommand(int UserId, AdminUpdateUserDto UserDto, int CurrentUserId) : IRequest<Result<bool>>;

public class AdminUpdateUserCommandHandler : IRequestHandler<AdminUpdateUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AdminUpdateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<bool>> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Get current user (the admin/librarian making the change)
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            return Result.Failure<bool>("Current user not found.");
        }
        
        // Check if current user has proper permissions
        bool hasPermission = false;
        
        if (currentUser.Role == UserRole.Admin)
        {
            // Admin can update anyone
            hasPermission = true;
        }
        else if (currentUser.Role == UserRole.Librarian)
        {
            // Get target user role
            var targetUser = await userRepository.GetAsync(u => u.Id == request.UserId);
            if (targetUser == null)
            {
                return Result.Failure<bool>($"User with ID {request.UserId} not found.");
            }
            
            // Librarians can only update Members, not other Librarians or Admins
            hasPermission = targetUser.Role == UserRole.Member;
            
            // Librarians can't change user roles
            if (targetUser.Role != request.UserDto.Role)
            {
                return Result.Failure<bool>("Librarians cannot change user roles.");
            }
        }
        
        if (!hasPermission)
        {
            return Result.Failure<bool>("You don't have permission to update this user.");
        }
        
        // Get the user to update
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        if (user == null)
        {
            return Result.Failure<bool>($"User with ID {request.UserId} not found.");
        }
        
        // Check if the email is changed and if so, check if it's already in use
        if (!user.Email.Equals(request.UserDto.Email, StringComparison.CurrentCultureIgnoreCase))
        {
            var emailExists = await userRepository.ExistsAsync(u => 
                u.Id != request.UserId && 
                u.Email.Equals(request.UserDto.Email, StringComparison.CurrentCultureIgnoreCase));
                
            if (emailExists)
            {
                return Result.Failure<bool>($"Email '{request.UserDto.Email}' is already in use.");
            }
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Update user information
            user.FullName = request.UserDto.FullName;
            user.Email = request.UserDto.Email;
            user.Role = request.UserDto.Role;
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Log the change with reason if provided
            if (!string.IsNullOrEmpty(request.UserDto.StatusChangeReason))
            {
                // Create an audit log entry if we had an AuditLog entity
                // For now, we'll just create a notification to the user
                var notificationRepository = _unitOfWork.Repository<Notification>();
                
                await notificationRepository.AddAsync(new Notification
                {
                    UserId = user.Id,
                    Type = NotificationType.SystemMaintenance,
                    Subject = "Account Updated",
                    Message = $"Your account information was updated by staff. Reason: {request.UserDto.StatusChangeReason}",
                    Status = NotificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"Failed to update user: {ex.Message}");
        }
    }
}