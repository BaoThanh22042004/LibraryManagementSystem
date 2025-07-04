using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using Application.Interfaces.Services;
using System.IO;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to create a new user by authorized staff (UC001)
/// This implements the user creation functionality:
/// - Enforces role-based permissions (BR-01): Only Admin or Librarian can create users
/// - Ensures proper role assignment (BR-02): Each user has one role
/// - Librarians can only create Member accounts; Admins can create both Librarian and Member accounts
/// - Creates role-specific records (Member or Librarian) with appropriate data
/// - Maintains audit trail
/// </summary>
public record CreateUserCommand(CreateUserDto UserDto, int CurrentUserId) : IRequest<Result<int>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Verify current user has permission to create users (BR-01)
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            return Result.Failure<int>("Current user not found.");
        }
        
        if (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.Librarian)
        {
            return Result.Failure<int>("Only administrators and librarians can create user accounts.");
        }
        
        // Check role creation permissions (BR-01)
        // Librarians can only create Member accounts
        if (currentUser.Role == UserRole.Librarian && request.UserDto.Role != UserRole.Member)
        {
            return Result.Failure<int>("Librarians can only create member accounts.");
        }
        
        // Check if email already exists
        var emailExists = await userRepository.ExistsAsync(u => 
            u.Email.Equals(request.UserDto.Email, StringComparison.CurrentCultureIgnoreCase));
            
        if (emailExists)
        {
            return Result.Failure<int>($"Email '{request.UserDto.Email}' is already registered.");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            
            // Create new user
            var user = _mapper.Map<User>(request.UserDto);
            
            // Hash the password (BR-05)
            user.PasswordHash = PasswordHasher.HashPassword(request.UserDto.Password);
            user.Status = UserStatus.Active; // Set default status
            
            await userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Create role-specific record if needed
            if (user.Role == UserRole.Member)
            {
                // Create Member record (UC001 Alternative Flow 1.1)
                var memberRepository = _unitOfWork.Repository<Member>();
                
                var member = new Member
                {
                    UserId = user.Id,
                    MembershipNumber = GenerateMembershipNumber(),
                    MembershipStartDate = DateTime.UtcNow,
                    MembershipStatus = MembershipStatus.Active,
                    OutstandingFines = 0,
                    CreatedAt = DateTime.UtcNow
                };
                
                await memberRepository.AddAsync(member);
            }
            else if (user.Role == UserRole.Librarian)
            {
                // Create Librarian record (UC001 Alternative Flow 1.2)
                var librarianRepository = _unitOfWork.Repository<Librarian>();
                
                var librarian = new Librarian
                {
                    UserId = user.Id,
                    EmployeeId = GenerateEmployeeId(),
                    HireDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                
                await librarianRepository.AddAsync(librarian);
            }
            
            // Create audit log entry (BR-22)
            var auditLogRepository = _unitOfWork.Repository<AuditLog>();
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Create,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Created user '{user.FullName}' with role '{user.Role}'",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            // Send invite email to the new user
            string webBaseUrl = _configuration["WebBaseUrl"] ?? "https://localhost:5001";
            string templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Application", "EmailTemplates", "UserInviteEmail.html");
            string emailBody = File.Exists(templatePath)
                ? File.ReadAllText(templatePath)
                : $@"
                    <h2>Welcome to the Library Management System</h2>
                    <p>Dear {user.FullName},</p>
                    <p>Your account has been created with the following details:</p>
                    <ul>
                        <li>Email: {user.Email}</li>
                        <li>Role: {user.Role}</li>
                    </ul>
                    <p>Please log in and change your password as soon as possible.</p>
                    <p>Regards,<br>Library Management System</p>
                ";
            emailBody = emailBody
                .Replace("{{FullName}}", user.FullName)
                .Replace("{{Email}}", user.Email)
                .Replace("{{Role}}", user.Role.ToString());
            // If a temporary password is set, include it
            if (!string.IsNullOrEmpty(request.UserDto.Password))
                emailBody = emailBody.Replace("{{TemporaryPassword}}", request.UserDto.Password);
            else
                emailBody = emailBody.Replace("{{TemporaryPassword}}", "");
            // If a password setup link is used, you can add logic here
            emailBody = emailBody.Replace("{{PasswordSetupLink}}", "");

            string emailSubject = "Your Library Account Has Been Created";
            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

            return Result.Success(user.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create user: {ex.Message}");
        }
    }
    
    private static string GenerateMembershipNumber()
    {
        // Generate a unique membership number (UC001 Alternative Flow 1.1)
        // Format: MEM-YYYYMMDD-XXXX where XXXX is random alphanumeric
        string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        string randomPart = Path.GetRandomFileName().Replace(".", "").Substring(0, 4).ToUpper();
        
        return $"MEM-{datePart}-{randomPart}";
    }
    
    private static string GenerateEmployeeId()
    {
        // Generate a unique employee ID (UC001 Alternative Flow 1.2)
        // Format: LIB-YYYYMMDD-XXXX where XXXX is random alphanumeric
        string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        string randomPart = Path.GetRandomFileName().Replace(".", "").Substring(0, 4).ToUpper();
        
        return $"LIB-{datePart}-{randomPart}";
    }
}