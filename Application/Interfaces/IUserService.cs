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
    /// <summary>
    /// Creates a new user in the system.
    /// Supports UC001 (Create User).
    /// </summary>
    /// <param name="request">User creation data</param>
    /// <param name="creatorId">ID of the user creating the account</param>
    /// <returns>Result with user ID if successful</returns>
    Task<Result<int>> CreateUserAsync(CreateUserRequest request, int creatorId);
    
    /// <summary>
    /// Gets detailed information about a user.
    /// Supports UC007 (View User Info).
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="requesterId">ID of the user requesting the information</param>
    /// <returns>Result with user details if successful</returns>
    Task<Result<UserDetailsResponse>> GetUserDetailsAsync(int id, int requesterId);
    
    /// <summary>
    /// Updates user information.
    /// Supports UC006 (Update User Info).
    /// </summary>
    /// <param name="request">User update data</param>
    /// <param name="updaterId">ID of the user performing the update</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> UpdateUserAsync(UpdateUserRequest request, int updaterId);
    
    /// <summary>
    /// Deletes a user from the system.
    /// Supports UC009 (Delete User).
    /// </summary>
    /// <param name="id">User ID to delete</param>
    /// <param name="deleterId">ID of the user performing the deletion</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteUserAsync(int id, int deleterId);
    
    /// <summary>
    /// Searches for users based on criteria.
    /// Supports UC007 (View User Info).
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <param name="requesterId">ID of the user performing the search</param>
    /// <returns>Result with paged list of users if successful</returns>
    Task<Result<UserSearchResponse>> SearchUsersAsync(UserSearchRequest request, int requesterId);
    
    /// <summary>
    /// Checks if the specified user has permission to perform actions on another user.
    /// Supports Business Rules: BR-01 (User Role Permissions), BR-03 (User Information Access).
    /// </summary>
    /// <param name="actorId">ID of the user performing the action</param>
    /// <param name="targetId">ID of the user being acted upon</param>
    /// <param name="action">Type of action being performed</param>
    /// <returns>Result indicating if the action is allowed</returns>
    Task<Result> CheckUserPermissionAsync(int actorId, int targetId, UserAction action);
    
    /// <summary>
    /// Checks if a user has the required role to perform an action.
    /// Supports Business Rules: BR-01, BR-02, BR-24.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="requiredRole">Minimum role required</param>
    /// <returns>Result indicating if the user has sufficient role</returns>
    Task<Result> CheckUserRoleAsync(int userId, UserRole requiredRole);
    
    /// <summary>
    /// Checks if a member can be deleted based on business rules.
    /// Supports Business Rule: BR-23 (Member Deletion Restriction).
    /// </summary>
    /// <param name="memberId">Member ID</param>
    /// <returns>Result with detailed validation results</returns>
    Task<Result<MemberDeletionValidationResult>> CanDeleteMemberAsync(int memberId);
}

/// <summary>
/// Actions that can be performed on users for permission checking.
/// </summary>
public enum UserAction
{
    View,
    Create,
    Update,
    Delete
}

/// <summary>
/// Validation results for member deletion checks.
/// </summary>
public class MemberDeletionValidationResult
{
    public bool CanDelete { get; set; }
    public bool HasActiveLoans { get; set; }
    public bool HasActiveReservations { get; set; }
    public bool HasUnpaidFines { get; set; }
    public string Message { get; set; } = string.Empty;
}
