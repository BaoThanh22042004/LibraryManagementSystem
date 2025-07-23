using Application.Common;
using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Service interface for user management operations.
/// Supports UC001 (Create User), UC006 (Update User Info), UC007 (View User Info), UC009 (Delete User).
/// </summary>
public interface IUserService
{
    Task<Result<int>> CreateUserAsync(CreateUserRequest request);
    
    Task<Result<UserDetailsDto>> GetUserDetailsAsync(int id);
    
    Task<Result> UpdateUserAsync(UpdateUserRequest request);
    
    Task<Result> DeleteUserAsync(int id);
    
    Task<Result<PagedResult<UserBasicDto>>> SearchUsersAsync(UserSearchRequest request);
    
    Task<Result> CheckUserPermissionAsync(int actorId, int targetId, UserAction action);
    
    Task<Result> CheckUserRoleAsync(int userId, UserRole requiredRole);
    
    Task<Result<MemberDeletionValidationDto>> CanDeleteMemberAsync(int memberId);

    Task<Result<UserDetailsDto>> GetUserDetailsByMemberIdAsync(int memberId);
    Task<Result<UserDetailsDto>> GetUserDetailsByEmailAsync(string email);
}

public enum UserAction
{
    View,
    Create,
    Update,
    Delete
}