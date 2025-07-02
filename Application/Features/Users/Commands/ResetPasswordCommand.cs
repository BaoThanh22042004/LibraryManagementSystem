using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

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
        
        // Check if passwords match
        if (request.ResetPasswordDto.NewPassword != request.ResetPasswordDto.ConfirmPassword)
        {
            return Result.Failure<bool>("The new password and confirmation password do not match.");
        }
        
        // Get user by email
        var user = await userRepository.GetAsync(u => u.Email.Equals(request.ResetPasswordDto.Email, StringComparison.CurrentCultureIgnoreCase));
        
        if (user == null)
        {
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        // Get token
        var resetToken = await resetTokenRepository.GetAsync(
            t => t.UserId == user.Id && 
                 t.Token == request.ResetPasswordDto.ResetToken && 
                 !t.IsUsed && 
                 t.ExpiresAt > DateTime.UtcNow);
                 
        if (resetToken == null)
        {
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Update user's password
            user.PasswordHash = PasswordHasher.HashPassword(request.ResetPasswordDto.NewPassword);
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Mark token as used
            resetToken.IsUsed = true;
            resetTokenRepository.Update(resetToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"Failed to reset password: {ex.Message}");
        }
    }
}