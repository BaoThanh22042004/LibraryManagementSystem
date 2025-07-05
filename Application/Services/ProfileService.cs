using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// Implementation of the profile service.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProfileService> _logger;

    // Constants
    private const int EMAIL_VERIFICATION_TOKEN_EXPIRY_HOURS = 24;

    public ProfileService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<ProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<ProfileResponse>> GetProfileAsync(int userId)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Id == userId);

            if (user == null)
            {
                return Result.Failure<ProfileResponse>("User not found");
            }

            var profileResponse = _mapper.Map<ProfileResponse>(user);
            return Result.Success(profileResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
            return Result.Failure<ProfileResponse>("An error occurred while retrieving your profile");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            // Get user
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Id == request.UserId);
            
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Capture before state for audit
            var beforeState = JsonSerializer.Serialize(new
            {
                user.FullName,
                user.Email,
                user.Phone,
                user.Address
            });

            // Update basic profile information
            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.LastModifiedAt = DateTime.UtcNow;

            // Check if email is being changed
            var emailChanged = !string.IsNullOrEmpty(request.Email) && 
                               !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase);
            
            if (emailChanged)
            {
                // Check if the new email is already in use
                var existingUser = await userRepo.GetAsync(u => 
                    u.Id != request.UserId && 
                    (u.Email == request.Email || u.PendingEmail == request.Email));
                
                if (existingUser != null)
                {
                    return Result.Failure("Email is already in use by another user");
                }

                // Store the new email as pending
                user.PendingEmail = request.Email;

                // Create verification token
                var tokenRepo = _unitOfWork.Repository<EmailVerificationToken>();
                
                // Invalidate any existing tokens
                var existingTokens = await tokenRepo.ListAsync(t => t.UserId == user.Id && !t.IsUsed);
                foreach (var token in existingTokens)
                {
                    token.IsUsed = true;
                    tokenRepo.Update(token);
                }

                // Create new verification token
                var verificationToken = new EmailVerificationToken
                {
                    UserId = user.Id,
                    Token = GenerateSecureToken(),
                    NewEmail = request.Email,
                    OldEmail = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddHours(EMAIL_VERIFICATION_TOKEN_EXPIRY_HOURS),
                    IsUsed = false
                };

                await tokenRepo.AddAsync(verificationToken);
                
                // Send verification email
                await _emailService.SendEmailVerificationAsync(
                    request.Email, 
                    user.FullName, 
                    verificationToken.Token);
            }

            // Save changes
            userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Capture after state for audit
            var afterState = JsonSerializer.Serialize(new
            {
                user.FullName,
                user.Email,
                user.PendingEmail,
                user.Phone,
                user.Address
            });

            // Log profile update
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                UserId = user.Id,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = "User updated their profile" + (emailChanged ? " (email change pending verification)" : ""),
                BeforeState = beforeState,
                AfterState = afterState,
                IsSuccess = true
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while updating your profile");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> VerifyEmailChangeAsync(VerifyEmailChangeRequest request)
    {
        try
        {
            // Find and validate token
            var tokenRepo = _unitOfWork.Repository<EmailVerificationToken>();
            var token = await tokenRepo.GetAsync(
                t => t.Token == request.Token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow,
                t => t.User);

            if (token == null)
            {
                return Result.Failure("Invalid or expired verification token");
            }

            if (token.NewEmail != request.Email)
            {
                return Result.Failure("Email mismatch in verification request");
            }

            // Get user
            var userRepo = _unitOfWork.Repository<User>();
            var user = token.User;

            // Check if email is still available
            var existingUser = await userRepo.GetAsync(u => 
                u.Id != user.Id && 
                u.Email == token.NewEmail);
            
            if (existingUser != null)
            {
                // Mark token as used to prevent reuse
                token.IsUsed = true;
                tokenRepo.Update(token);
                await _unitOfWork.SaveChangesAsync();
                
                return Result.Failure("Email is already in use by another user");
            }

            // Update user email
            var oldEmail = user.Email;
            user.Email = token.NewEmail;
            user.PendingEmail = null;
            user.LastModifiedAt = DateTime.UtcNow;
            
            // Mark token as used
            token.IsUsed = true;
            
            // Save changes
            userRepo.Update(user);
            tokenRepo.Update(token);
            await _unitOfWork.SaveChangesAsync();

            // Log email verification
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                UserId = user.Id,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"User verified email change from {oldEmail} to {user.Email}",
                BeforeState = JsonSerializer.Serialize(new { Email = oldEmail }),
                AfterState = JsonSerializer.Serialize(new { Email = user.Email }),
                IsSuccess = true
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email change for email {Email}", request.Email);
            return Result.Failure("An error occurred while verifying your email change");
        }
    }

    #region Helper Methods

    private string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token
        byte[] tokenData = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenData);
        }

        // Convert to URL-safe base64 string
        return Convert.ToBase64String(tokenData)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    #endregion
}
