using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to handle the forgot password request (UC005)
/// This implements the first part of the password reset functionality:
/// - Receives email address from user
/// - Generates a secure reset token
/// - Sends email with reset link
/// - Applies rate limiting (max 3 requests per hour)
/// - Maintains audit trail
/// </summary>
public record ForgotPasswordCommand(ForgotPasswordDto ForgotPasswordDto) : IRequest<Result<bool>>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly int _tokenExpirationMinutes;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork, 
        IEmailService emailService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _tokenExpirationMinutes = 60; // Default 60 minutes expiration (UC005 requirement)
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var resetTokenRepository = _unitOfWork.Repository<PasswordResetToken>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Get user by email
        var user = await userRepository.GetAsync(u => u.Email.Equals(request.ForgotPasswordDto.Email, StringComparison.CurrentCultureIgnoreCase));
        
        if (user == null)
        {
            // For security reasons, don't reveal whether the email exists (BR-04)
            // But log attempted password reset for non-existent account (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = null, // No user found
                ActionType = AuditActionType.PasswordReset,
                EntityType = "User",
                EntityId = null,
                EntityName = null,
                Details = $"Password reset attempt for non-existent email: {request.ForgotPasswordDto.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = "User not found"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Return success even though user doesn't exist (for security)
            return Result.Success(true);
        }
        
        // Check rate limiting (BR-26: Maximum 3 reset requests per email per hour)
        var lastHour = DateTime.UtcNow.AddHours(-1);
        var recentRequests = await resetTokenRepository.CountAsync(t => 
            t.UserId == user.Id && 
            t.CreatedAt > lastHour);
            
        if (recentRequests >= 3)
        {
            // Log rate limit exceeded (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                ActionType = AuditActionType.PasswordReset,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Password reset rate limit exceeded for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = "Rate limit exceeded: Maximum 3 requests per hour"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>("Too many password reset requests. Please try again later.");
        }

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Generate secure token
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
            
            // Log password reset request (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                ActionType = AuditActionType.PasswordReset,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Password reset token generated for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Send email with reset link (UC005 requirement)
            string webBaseUrl = _configuration["WebBaseUrl"] ?? "https://localhost:5001";
            string resetLink = $"{webBaseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            // Load email template
            string templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Application", "EmailTemplates", "PasswordResetEmail.html");
            string emailBody = File.Exists(templatePath)
                ? File.ReadAllText(templatePath)
                : $@"
                    <h2>Password Reset Request</h2>
                    <p>Dear {user.FullName},</p>
                    <p>We received a request to reset your password. Click the link below to set a new password:</p>
                    <p><a href='{resetLink}'>Reset Your Password</a></p>
                    <p>This link will expire in {_tokenExpirationMinutes} minutes.</p>
                    <p>If you didn't request a password reset, you can ignore this email.</p>
                    <p>Regards,<br>Library Management System</p>
                ";
            emailBody = emailBody
                .Replace("{{FullName}}", user.FullName)
                .Replace("{{ResetLink}}", resetLink)
                .Replace("{{ExpirationMinutes}}", _tokenExpirationMinutes.ToString());

            string emailSubject = "Password Reset Request";
            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
            
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
                ActionType = AuditActionType.PasswordReset,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Failed to process password reset request for user {user.Email}",
                Module = "PasswordReset",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>($"Failed to process password reset request: {ex.Message}");
        }
    }
}