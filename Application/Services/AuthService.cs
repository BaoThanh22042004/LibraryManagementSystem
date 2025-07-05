using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services;

/// <summary>
/// Implementation of the authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    // Constants
    private const int MAX_FAILED_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_DURATION_MINUTES = 15;
    private const int PASSWORD_RESET_TOKEN_EXPIRY_HOURS = 1;
    private const int MAX_RESET_REQUESTS_PER_HOUR = 3;

    public AuthService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            // Get user by email
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Email == request.Email);

            if (user == null)
            {
                await _auditService.LogAuthenticationAsync(
                    null,
                    AuditActionType.LoginFailed,
                    false,
                    $"Login failed for email {request.Email}: User not found",
                    null,
                    "User not found");

                return Result.Failure<LoginResponse>("Invalid email or password");
            }

            // Check account status
            if (user.Status != UserStatus.Active)
            {
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.LoginFailed,
                    false,
                    $"Login failed for user {user.Id}: Account not active",
                    null,
                    $"Account status is {user.Status}");

                return Result.Failure<LoginResponse>($"Account is {user.Status.ToString().ToLower()}");
            }

            // Check lockout status
            var lockoutResult = await CheckAccountLockoutStatusAsync(request.Email);
            if (lockoutResult.IsSuccess && lockoutResult.Value.HasValue)
            {
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.LoginFailed,
                    false,
                    $"Login failed for user {user.Id}: Account locked",
                    null,
                    $"Account locked until {lockoutResult.Value}");

                var remainingMinutes = Math.Ceiling((lockoutResult.Value.Value - DateTime.UtcNow).TotalMinutes);
                return Result.Failure<LoginResponse>($"Account is temporarily locked. Try again in {remainingMinutes} minutes");
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                var failedResult = await RecordFailedLoginAttemptAsync(request.Email);
                
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.LoginFailed,
                    false,
                    $"Login failed for user {user.Id}: Invalid password",
                    null,
                    "Invalid password");

                if (failedResult.IsSuccess && failedResult.Value.HasValue)
                {
                    var remainingMinutes = Math.Ceiling((failedResult.Value.Value - DateTime.UtcNow).TotalMinutes);
                    return Result.Failure<LoginResponse>($"Account is temporarily locked. Try again in {remainingMinutes} minutes");
                }

                return Result.Failure<LoginResponse>("Invalid email or password");
            }

            // Clear failed login attempts
            await ClearFailedLoginAttemptsAsync(user.Id);

            // Create login response
            var loginResponse = _mapper.Map<LoginResponse>(user);

            // Log successful login
            await _auditService.LogAuthenticationAsync(
                user.Id,
                AuditActionType.Login,
                true,
                $"User {user.Id} logged in successfully");

            return Result.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", request.Email);
            return Result.Failure<LoginResponse>("An error occurred during login");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> RegisterMemberAsync(RegisterRequest request)
    {
        try
        {
            // Validate the password
            var passwordValidation = ValidatePassword(request.Password);
            if (passwordValidation.IsFailure)
            {
                return Result.Failure<int>(passwordValidation.Error);
            }

            await _unitOfWork.BeginTransactionAsync();

            // Check if email already exists
            var userRepo = _unitOfWork.Repository<User>();
            var existingUser = await userRepo.GetAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure<int>("Email is already registered");
            }

            // Create user
            var user = _mapper.Map<User>(request);
            user.PasswordHash = PasswordHasher.HashPassword(request.Password);

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Create member
            var memberRepo = _unitOfWork.Repository<Member>();
            var member = new Member
            {
                UserId = user.Id,
                MembershipNumber = !string.IsNullOrWhiteSpace(request.PreferredMembershipNumber) 
                    ? request.PreferredMembershipNumber 
                    : GenerateMembershipNumber(),
                MembershipStartDate = DateTime.UtcNow,
                MembershipStatus = MembershipStatus.Active,
                OutstandingFines = 0
            };

            // Ensure membership number is unique
            while (await memberRepo.ExistsAsync(m => m.MembershipNumber == member.MembershipNumber))
            {
                member.MembershipNumber = GenerateMembershipNumber();
            }

            await memberRepo.AddAsync(member);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Log registration
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                UserId = user.Id,
                ActionType = AuditActionType.Create,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = "User self-registered as Member",
                IsSuccess = true
            });

            return Result.Success(user.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error registering new member with email {Email}", request.Email);
            return Result.Failure<int>("An error occurred during registration");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
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

            // Verify current password
            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.PasswordChangeFailed,
                    false,
                    $"Password change failed for user {user.Id}: Current password invalid",
                    null,
                    "Current password is incorrect");

                return Result.Failure("Current password is incorrect");
            }

            // Validate new password
            var passwordValidation = ValidatePassword(request.NewPassword);
            if (passwordValidation.IsFailure)
            {
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.PasswordChangeFailed,
                    false,
                    $"Password change failed for user {user.Id}: New password invalid",
                    null,
                    passwordValidation.Error);

                return Result.Failure(passwordValidation.Error);
            }

            // Change password
            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.LastModifiedAt = DateTime.UtcNow;
            userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Log password change
            await _auditService.LogAuthenticationAsync(
                user.Id,
                AuditActionType.PasswordChanged,
                true,
                $"Password changed for user {user.Id}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while changing password");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> RequestPasswordResetAsync(ResetPasswordRequest request)
    {
        try
        {
            // Check if email exists (don't reveal existence of email to caller)
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Return success for security reasons even if user doesn't exist
                _logger.LogInformation("Password reset requested for non-existent email {Email}", request.Email);
                return Result.Success();
            }

            // Check rate limiting
            var resetTokenRepo = _unitOfWork.Repository<PasswordResetToken>();
            var hourAgo = DateTime.UtcNow.AddHours(-1);
            var recentRequests = await resetTokenRepo.CountAsync(t => 
                t.User.Email == request.Email && 
                t.CreatedAt > hourAgo);

            if (recentRequests >= MAX_RESET_REQUESTS_PER_HOUR)
            {
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.PasswordReset,
                    false,
                    $"Password reset rate limit exceeded for user {user.Id}",
                    null,
                    "Rate limit exceeded");

                return Result.Failure($"Too many reset requests. Please try again later.");
            }

            // Invalidate existing tokens
            var existingTokens = await resetTokenRepo.ListAsync(t => t.UserId == user.Id && !t.IsUsed);
            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                resetTokenRepo.Update(token);
            }
            await _unitOfWork.SaveChangesAsync();

            // Create new token
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = GenerateSecureToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(PASSWORD_RESET_TOKEN_EXPIRY_HOURS),
                IsUsed = false
            };

            await resetTokenRepo.AddAsync(resetToken);
            await _unitOfWork.SaveChangesAsync();

            // Send email
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken.Token);

            // Log token creation
            await _auditService.LogAuthenticationAsync(
                user.Id,
                AuditActionType.PasswordReset,
                true,
                $"Password reset requested for user {user.Id}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset for email {Email}", request.Email);
            return Result.Failure("An error occurred while processing your request");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ConfirmPasswordResetAsync(ConfirmResetPasswordRequest request)
    {
        try
        {
            // Validate the password
            var passwordValidation = ValidatePassword(request.NewPassword);
            if (passwordValidation.IsFailure)
            {
                return Result.Failure(passwordValidation.Error);
            }

            // Find and validate token
            var resetTokenRepo = _unitOfWork.Repository<PasswordResetToken>();
            var resetToken = await resetTokenRepo.GetAsync(
                t => t.Token == request.Token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow,
                t => t.User);

            if (resetToken == null)
            {
                return Result.Failure("Invalid or expired reset token");
            }

            if (resetToken.User.Email != request.Email)
            {
                return Result.Failure("Invalid reset token");
            }

            // Update user password
            var userRepo = _unitOfWork.Repository<User>();
            var user = resetToken.User;
            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.LastModifiedAt = DateTime.UtcNow;
            
            // Clear any lockout
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
            
            userRepo.Update(user);

            // Mark token as used
            resetToken.IsUsed = true;
            resetTokenRepo.Update(resetToken);
            
            await _unitOfWork.SaveChangesAsync();

            // Log password reset
            await _auditService.LogAuthenticationAsync(
                user.Id,
                AuditActionType.PasswordChanged,
                true,
                $"Password reset completed for user {user.Id}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset for email {Email}", request.Email);
            return Result.Failure("An error occurred while resetting your password");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<DateTime?>> RecordFailedLoginAttemptAsync(string email)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Email == email);

            if (user == null)
            {
                return Result.Success<DateTime?>(null);
            }

            user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;
            
            // Check if account should be locked
            if (user.FailedLoginAttempts >= MAX_FAILED_LOGIN_ATTEMPTS)
            {
                user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                
                await _auditService.LogAuthenticationAsync(
                    user.Id,
                    AuditActionType.AccountLocked,
                    true,
                    $"Account locked for user {user.Id} due to too many failed login attempts",
                    null);
            }

            userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success(user.LockoutEndTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed login attempt for email {Email}", email);
            return Result.Failure<DateTime?>("An error occurred while processing login attempt");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<DateTime?>> CheckAccountLockoutStatusAsync(string email)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Email == email);

            if (user == null || !user.LockoutEndTime.HasValue)
            {
                return Result.Success<DateTime?>(null);
            }

            // If lockout has expired, clear it
            if (user.LockoutEndTime < DateTime.UtcNow)
            {
                user.LockoutEndTime = null;
                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return Result.Success<DateTime?>(null);
            }

            return Result.Success(user.LockoutEndTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account lockout status for email {Email}", email);
            return Result.Failure<DateTime?>("An error occurred while checking account status");
        }
    }

    /// <inheritdoc/>
    public async Task ClearFailedLoginAttemptsAsync(int userId)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Id == userId);

            if (user == null)
            {
                return;
            }

            if (user.FailedLoginAttempts > 0 || user.LockoutEndTime.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndTime = null;
                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing failed login attempts for user {UserId}", userId);
        }
    }

    /// <inheritdoc/>
    public Result ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure("Password cannot be empty");
        }

        if (password.Length < 8)
        {
            return Result.Failure("Password must be at least 8 characters long");
        }

        // Check for complexity requirements
        bool hasUpperCase = password.Any(char.IsUpper);
        bool hasLowerCase = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        var missingRequirements = new List<string>();

        if (!hasUpperCase) missingRequirements.Add("at least one uppercase letter");
        if (!hasLowerCase) missingRequirements.Add("at least one lowercase letter");
        if (!hasDigit) missingRequirements.Add("at least one digit");
        if (!hasSpecialChar) missingRequirements.Add("at least one special character");

        if (missingRequirements.Count > 0)
        {
            return Result.Failure($"Password must contain {string.Join(", ", missingRequirements)}");
        }

        return Result.Success();
    }

    #region Helper Methods
    private static string GenerateSecureToken()
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

    private static string GenerateMembershipNumber()
    {
        // Generate a random membership number with format: LIB-YYYYMMDD-XXXX
        // Where YYYYMMDD is the current date and XXXX is a random number
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random();
        var randomPart = random.Next(1000, 10000).ToString();
        
        return $"LIB-{date}-{randomPart}";
    }

    #endregion
}
