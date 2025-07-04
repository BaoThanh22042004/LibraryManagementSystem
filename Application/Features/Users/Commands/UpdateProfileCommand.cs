using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to update a user's profile information (UC003)
/// This implements the profile update functionality:
/// - Users can update their own basic information
/// - Ensures email uniqueness
/// - Maintains audit trail
/// </summary>
public record UpdateProfileCommand(int UserId, UpdateProfileDto ProfileDto) : IRequest<Result<bool>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Get user by ID
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        
        if (user == null)
        {
            return Result.Failure<bool>($"User with ID {request.UserId} not found.");
        }
        
        // Store the original values for audit logging
        var originalName = user.FullName;
        var originalEmail = user.Email;
        var originalPhone = user.Phone;
        var originalAddress = user.Address;
        
        // Check if the email is changed and if so, check if it's already in use (UC003 Exception 3.0.E1)
        if (!user.Email.Equals(request.ProfileDto.Email, StringComparison.CurrentCultureIgnoreCase))
        {
            var emailExists = await userRepository.ExistsAsync(u => 
                u.Id != request.UserId && 
                u.Email.Equals(request.ProfileDto.Email, StringComparison.CurrentCultureIgnoreCase));
                
            if (emailExists)
            {
                // Log failed update attempt (BR-22)
                await auditLogRepository.AddAsync(new AuditLog
                {
                    UserId = request.UserId,
                    ActionType = AuditActionType.Update,
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    EntityName = user.FullName,
                    Details = $"Failed to update profile due to email '{request.ProfileDto.Email}' already in use",
                    Module = "UserProfile",
                    IsSuccess = false,
                    ErrorMessage = "Email already in use"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return Result.Failure<bool>($"Email '{request.ProfileDto.Email}' is already in use.");
            }
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Update the user profile
            user.FullName = request.ProfileDto.FullName;
            user.Email = request.ProfileDto.Email;
            user.Phone = request.ProfileDto.Phone;
            user.Address = request.ProfileDto.Address;
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            
            // Log successful profile update (BR-22)
            var changes = new List<string>();
            if (originalName != user.FullName)
                changes.Add($"Name changed from '{originalName}' to '{user.FullName}'");
            if (originalEmail != user.Email)
                changes.Add($"Email changed from '{originalEmail}' to '{user.Email}'");
            if (originalPhone != user.Phone)
                changes.Add($"Phone changed from '{originalPhone}' to '{user.Phone}'");
            if (originalAddress != user.Address)
                changes.Add($"Address changed from '{originalAddress}' to '{user.Address}'");
                
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.UserId,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"User profile updated: {string.Join(", ", changes)}",
                Module = "UserProfile",
                BeforeState = $"{{\"FullName\":\"{originalName}\",\"Email\":\"{originalEmail}\",\"Phone\":\"{originalPhone}\",\"Address\":\"{originalAddress}\"}}",
                AfterState = $"{{\"FullName\":\"{user.FullName}\",\"Email\":\"{user.Email}\",\"Phone\":\"{user.Phone}\",\"Address\":\"{user.Address}\"}}",
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
                UserId = request.UserId,
                ActionType = AuditActionType.Update,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Failed to update profile for user {user.Email}",
                Module = "UserProfile",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<bool>($"Failed to update profile: {ex.Message}");
        }
    }
}