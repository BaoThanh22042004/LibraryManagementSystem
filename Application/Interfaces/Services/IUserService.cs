using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPaginatedUsersAsync(PagedRequest request, string? searchTerm = null);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<int> CreateUserAsync(CreateUserDto userDto);
    Task UpdateUserAsync(int id, UpdateUserDto userDto);
    Task DeleteUserAsync(int id);
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
}