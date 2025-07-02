using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

public record ForgotPasswordCommand(ForgotPasswordDto ForgotPasswordDto) : IRequest<Result<bool>>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly int _tokenExpirationMinutes;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork, 
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _tokenExpirationMinutes = 60; // Default 60 minutes
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var resetTokenRepository = _unitOfWork.Repository<PasswordResetToken>();
        
        // Get user by email
        var user = await userRepository.GetAsync(u => u.Email.Equals(request.ForgotPasswordDto.Email, StringComparison.CurrentCultureIgnoreCase));
        
        if (user == null)
        {
            // For security reasons, don't reveal whether the email exists
            return Result.Success(true);
        }
        
        // Check rate limiting (BR-26: Maximum 3 reset requests per email per hour)
        var lastHour = DateTime.UtcNow.AddHours(-1);
        var recentRequests = await resetTokenRepository.CountAsync(t => 
            t.UserId == user.Id && 
            t.CreatedAt > lastHour);
            
        if (recentRequests >= 3)
        {
            return Result.Failure<bool>("Too many password reset requests. Please try again later.");
        }

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Generate token
            string token = TokenGenerator.GenerateToken();
            
            // Create reset token entry
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
                IsUsed = false
            };
            
            await resetTokenRepository.AddAsync(resetToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Send email with reset link
            string resetLink = $"/Account/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            string emailSubject = "Password Reset Request";
            string emailBody = $@"
                <h2>Password Reset Request</h2>
                <p>Dear {user.FullName},</p>
                <p>We received a request to reset your password. Click the link below to set a new password:</p>
                <p><a href='{resetLink}'>Reset Your Password</a></p>
                <p>This link will expire in {_tokenExpirationMinutes} minutes.</p>
                <p>If you didn't request a password reset, you can ignore this email.</p>
                <p>Regards,<br>Library Management System</p>
            ";
            
            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<bool>($"Failed to process password reset request: {ex.Message}");
        }
    }
}