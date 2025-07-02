using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to change a user's password (UC004)
/// This implements the password change functionality as described in UC004, enforcing:
/// - Current password verification for security
/// - New password must meet complexity requirements (validated by ChangePasswordDtoValidator)
/// - Password changes are audited
/// - All changes are transactional
/// </summary>
public record ChangePasswordCommand(int UserId, ChangePasswordDto PasswordDto) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Validate new password match
        if (request.PasswordDto.NewPassword != request.PasswordDto.ConfirmNewPassword)
        {
            return Result.Failure("New password and confirmation password do not match.");
        }
        
        // Get current user using GetAsync instead of GetByIdAsync
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        
        if (user == null)
        {
            return Result.Failure($"User with ID {request.UserId} not found.");
        }
        
        // Verify current password (UC004 requirement)
        if (!PasswordHasher.VerifyPassword(request.PasswordDto.CurrentPassword, user.PasswordHash))
        {
            // Log failed password change attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.UserId,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = "Failed password change attempt - current password incorrect",
                Module = "UserProfile",
                IsSuccess = false,
                ErrorMessage = "Current password incorrect"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure("Current password is incorrect.");
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Update password hash (BR-05)
            user.PasswordHash = PasswordHasher.HashPassword(request.PasswordDto.NewPassword);
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Log successful password change (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.UserId,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = "User successfully changed password",
                Module = "UserProfile",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            
            // Log failure outside transaction (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.UserId,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = "Failed to change password",
                Module = "UserProfile",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure($"Failed to change password: {ex.Message}");
        }
    }
}