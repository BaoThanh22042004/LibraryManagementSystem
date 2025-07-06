using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for user profile operations.
/// Supports UC003 (Update Profile).
/// </summary>
public interface IProfileService
{
    Task<Result<ProfileDto>> GetProfileAsync(int userId);
    
    Task<Result> UpdateProfileAsync(UpdateProfileRequest request);
    
    Task<Result> VerifyEmailChangeAsync(VerifyEmailChangeRequest request);
}
