using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

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

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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