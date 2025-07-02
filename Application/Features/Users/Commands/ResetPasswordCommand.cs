using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to handle the password reset completion (UC005)
/// This implements the second part of the password reset functionality:
/// - Validates the reset token
/// - Ensures token has not expired
/// - Sets the new password
/// - Enforces password complexity requirements (via validator)
/// - Invalidates the token after use
/// - Maintains audit trail
/// </summary>
public record ResetPasswordCommand(ResetPasswordDto ResetPasswordDto) : IRequest<Result<bool>>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var resetTokenRepository = _unitOfWork.Repository<PasswordResetToken>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Check if passwords match
        if (request.ResetPasswordDto.NewPassword != request.ResetPasswordDto.ConfirmPassword)
        {
            return Result.Failure<bool>("The new password and confirmation password do not match.");
        }
        
        // Get user by email
        var user = await userRepository.GetAsync(u => u.Email.Equals(request.ResetPasswordDto.Email, StringComparison.CurrentCultureIgnoreCase));
        
        if (user == null)
        {
            // Log attempted password reset with invalid email (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = null,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = null,
                EntityName = null,
                Details = $"Password reset attempt with invalid email: {request.ResetPasswordDto.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = "Invalid email or token"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        // Get token and validate it (UC005 requirement)
        var resetToken = await resetTokenRepository.GetAsync(
            t => t.UserId == user.Id && 
                 t.Token == request.ResetPasswordDto.ResetToken && 
                 !t.IsUsed && 
                 t.ExpiresAt > DateTime.UtcNow);
                 
        if (resetToken == null)
        {
            // Log attempted password reset with invalid token (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Password reset attempt with invalid or expired token for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = "Invalid or expired token"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Update user's password (BR-05)
            user.PasswordHash = PasswordHasher.HashPassword(request.ResetPasswordDto.NewPassword);
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Mark token as used to prevent reuse (security best practice)
            resetToken.IsUsed = true;
            resetTokenRepository.Update(resetToken);
            
            // Invalidate all other active tokens for this user (security best practice)
            var otherTokens = await resetTokenRepository.ListAsync(
                t => t.UserId == user.Id && 
                     !t.IsUsed && 
                     t.Id != resetToken.Id);
                     
            foreach (var token in otherTokens)
            {
                token.IsUsed = true;
                resetTokenRepository.Update(token);
            }
            
            // Create audit log for successful password reset (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Password successfully reset for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = true
            });
            
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
                UserId = user.Id,
                ActionType = AuditActionType.PasswordChange,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Failed to reset password for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>($"Failed to reset password: {ex.Message}");
        }
    }
}