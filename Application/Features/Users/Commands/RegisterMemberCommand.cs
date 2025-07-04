using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to handle member self-registration (UC008)
/// This implements the member registration functionality:
/// - Creates user account with Member role
/// - Creates member profile with unique membership number
/// - Supports preferred membership numbers
/// - Maintains audit trail
/// - Ensures email uniqueness
/// </summary>
public record RegisterMemberCommand(RegisterMemberDto RegisterDto) : IRequest<Result<int>>;

public class RegisterMemberCommandHandler : IRequestHandler<RegisterMemberCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(RegisterMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Begin transaction to ensure both User and Member are created atomically
            await _unitOfWork.BeginTransactionAsync();
            
            var userRepository = _unitOfWork.Repository<User>();
            var memberRepository = _unitOfWork.Repository<Member>();
            
            // Check if email already exists
            var emailExists = await userRepository.ExistsAsync(u => 
                u.Email.Equals(request.RegisterDto.Email, StringComparison.CurrentCultureIgnoreCase));
                
            if (emailExists)
            {
                return Result.Failure<int>($"Email '{request.RegisterDto.Email}' is already registered.");
            }
            
            // Check if custom membership number is provided and verify uniqueness
            string membershipNumber = request.RegisterDto.PreferredMembershipNumber ?? GenerateMembershipNumber();
            
            if (!string.IsNullOrEmpty(request.RegisterDto.PreferredMembershipNumber))
            {
                var membershipNumberExists = await memberRepository.ExistsAsync(m => 
                    m.MembershipNumber.Equals(membershipNumber, StringComparison.CurrentCultureIgnoreCase));
                    
                if (membershipNumberExists)
                {
                    // Auto-generate a membership number if the preferred one is taken
                    membershipNumber = GenerateMembershipNumber();
                }
            }
            
            // Create new user with Member role (BR-02)
            var user = new User
            {
                FullName = request.RegisterDto.FullName,
                Email = request.RegisterDto.Email,
                PasswordHash = PasswordHasher.HashPassword(request.RegisterDto.Password), // BR-05
                Role = UserRole.Member,
                Status = UserStatus.Active,
                Phone = request.RegisterDto.Phone,
                Address = request.RegisterDto.Address,
                CreatedAt = DateTime.UtcNow
            };
            
            await userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Create member profile with unique membership number
            var member = new Member
            {
                UserId = user.Id,
                MembershipNumber = membershipNumber,
                MembershipStartDate = DateTime.UtcNow, // Set start date to current date (UC008)
                MembershipStatus = MembershipStatus.Active, // Set status to Active (UC008)
                OutstandingFines = 0,
                CreatedAt = DateTime.UtcNow
            };
            
            await memberRepository.AddAsync(member);
            
            // Create audit log for the registration (BR-22)
            var auditLogRepository = _unitOfWork.Repository<AuditLog>();
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id, // Self-registration, so user is creating their own account
                ActionType = AuditActionType.Create,
                EntityType = "Member",
                EntityId = member.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Self-registration of member '{user.FullName}' with membership number '{membershipNumber}'",
                Module = "Registration",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(user.Id);
        }
        catch (Exception ex)
        {
            // Rollback transaction if any error occurs
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to register member: {ex.Message}");
        }
    }
    
    private static string GenerateMembershipNumber()
    {
        // Generate a unique membership number
        // Format: MEM-YYYYMMDD-XXXX where XXXX is random alphanumeric
        string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        string randomPart = Path.GetRandomFileName().Replace(".", "").Substring(0, 4).ToUpper();
        
        return $"MEM-{datePart}-{randomPart}";
    }
}