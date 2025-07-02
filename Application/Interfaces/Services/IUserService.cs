using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPaginatedUsersAsync(PagedRequest request, string? searchTerm = null);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Creates a new user with the provided details (UC001)
    /// </summary>
    /// <param name="userDto">User creation data</param>
    /// <param name="currentUserId">ID of the current user performing the action</param>
    /// <returns>The ID of the created user</returns>
    Task<int> CreateUserAsync(CreateUserDto userDto, int currentUserId);

    Task UpdateUserAsync(int id, UpdateUserDto userDto);

    /// <summary>
    /// Deletes a user account with proper authorization checks
    /// </summary>
    /// <param name="id">ID of the user to delete</param>
    /// <param name="currentUserId">ID of the current user performing the action</param>
    Task DeleteUserAsync(int id, int currentUserId);

    Task<bool> ExistsAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
    Task<Result<bool>> UpdateProfileAsync(int userId, UpdateProfileDto profileDto);
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<Result<bool>> ValidateResetTokenAsync(string email, string token);
    Task<Result<bool>> AdminUpdateUserAsync(int id, AdminUpdateUserDto userDto, int currentUserId);
    Task<UserDetailsDto?> GetUserDetailsAsync(int id);
    Task<Result<bool>> ValidateUserDeletionAsync(int id);

    /// <summary>
    /// Registers a new member with the system through self-registration (UC008)
    /// </summary>
    /// <param name="registerDto">The registration information</param>
    /// <returns>Result with the newly created user ID or error message</returns>
    Task<Result<int>> RegisterMemberAsync(RegisterMemberDto registerDto);
}