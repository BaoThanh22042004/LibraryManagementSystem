using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to update user information by authorized staff (UC006)
/// This implements the staff-managed user update functionality:
/// - Enforces role-based permissions (BR-01): Only Admin or Librarian can update users
/// - Applies permission hierarchy: Admins can update anyone, Librarians can only update Members
/// - Maintains audit trail (BR-22)
/// - Ensures email uniqueness
/// - Allows status change reasons to be recorded
/// </summary>
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
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Get current user (the admin/librarian making the change)
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            return Result.Failure<bool>("Current user not found.");
        }
        
        // Get the user to update
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        if (user == null)
        {
            return Result.Failure<bool>($"User with ID {request.UserId} not found.");
        }
        
        // Store original values for audit logging
        var originalFullName = user.FullName;
        var originalEmail = user.Email;
        var originalRole = user.Role;
        var originalStatus = user.Status;
        var originalPhone = user.Phone;
        var originalAddress = user.Address;
        
        // Check if current user has proper permissions (UC006 Exception 6.0.E2)
        bool hasPermission = false;
        
        if (currentUser.Role == UserRole.Admin)
        {
            // Admin can update anyone (BR-01)
            hasPermission = true;
        }
        else if (currentUser.Role == UserRole.Librarian)
        {
            // Librarians can only update Members, not other Librarians or Admins (BR-01)
            hasPermission = user.Role == UserRole.Member;
            
            // Librarians can't change user roles
            if (user.Role != request.UserDto.Role)
            {
                // Log unauthorized role change attempt (BR-22)
                await auditLogRepository.AddAsync(new AuditLog
                {
                    UserId = request.CurrentUserId,
                    ActionType = AuditActionType.Update,
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    EntityName = user.FullName,
                    Details = $"Librarian attempted to change role of user {user.Email} from {user.Role} to {request.UserDto.Role}",
                    Module = "UserManagement",
                    IsSuccess = false,
                    ErrorMessage = "Librarians cannot change user roles"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return Result.Failure<bool>("Librarians cannot change user roles.");
            }
        }
        
        if (!hasPermission)
        {
            // Log unauthorized update attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Unauthorized user update attempt on {user.Email} by user {request.CurrentUserId}",
                Module = "UserManagement",
                IsSuccess = false,
                ErrorMessage = "Insufficient permissions"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>("You don't have permission to update this user.");
        }
        
        // Check if the email is changed and if so, check if it's already in use (UC006 Exception 6.0.E3)
        if (!user.Email.Equals(request.UserDto.Email, StringComparison.CurrentCultureIgnoreCase))
        {
            var emailExists = await userRepository.ExistsAsync(u => 
                u.Id != request.UserId && 
                u.Email.Equals(request.UserDto.Email, StringComparison.CurrentCultureIgnoreCase));
                
            if (emailExists)
            {
                // Log failed update attempt due to duplicate email (BR-22)
                await auditLogRepository.AddAsync(new AuditLog
                {
                    UserId = request.CurrentUserId,
                    ActionType = AuditActionType.Update,
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    EntityName = user.FullName,
                    Details = $"Failed to update user {user.Email} - email '{request.UserDto.Email}' already in use",
                    Module = "UserManagement",
                    IsSuccess = false,
                    ErrorMessage = "Email already in use"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
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
            user.Status = request.UserDto.Status;
            user.Phone = request.UserDto.Phone;
            user.Address = request.UserDto.Address;
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Build change summary for audit log
            var changes = new List<string>();
            if (originalFullName != user.FullName)
                changes.Add($"Name changed from '{originalFullName}' to '{user.FullName}'");
            if (originalEmail != user.Email)
                changes.Add($"Email changed from '{originalEmail}' to '{user.Email}'");
            if (originalRole != user.Role)
                changes.Add($"Role changed from '{originalRole}' to '{user.Role}'");
            if (originalStatus != user.Status)
                changes.Add($"Status changed from '{originalStatus}' to '{user.Status}'");
            if (originalPhone != user.Phone)
                changes.Add($"Phone changed from '{originalPhone}' to '{user.Phone}'");
            if (originalAddress != user.Address)
                changes.Add($"Address changed from '{originalAddress}' to '{user.Address}'");
            
            // Log the audit trail for this update (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"User updated: {string.Join(", ", changes)}",
                BeforeState = $"{{\"FullName\":\"{originalFullName}\",\"Email\":\"{originalEmail}\",\"Role\":\"{originalRole}\",\"Status\":\"{originalStatus}\",\"Phone\":\"{originalPhone}\",\"Address\":\"{originalAddress}\"}}",
                AfterState = $"{{\"FullName\":\"{user.FullName}\",\"Email\":\"{user.Email}\",\"Role\":\"{user.Role}\",\"Status\":\"{user.Status}\",\"Phone\":\"{user.Phone}\",\"Address\":\"{user.Address}\"}}",
                Module = "UserManagement",
                IsSuccess = true
            });
            
            // Create notification for the user with change reason if provided
            if (!string.IsNullOrEmpty(request.UserDto.StatusChangeReason))
            {
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
            
            // Log failure outside transaction (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Failed to update user {user.Email}",
                Module = "UserManagement",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>($"Failed to update user: {ex.Message}");
        }
    }
}