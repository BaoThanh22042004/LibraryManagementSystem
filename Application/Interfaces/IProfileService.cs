using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for user profile operations.
/// Supports UC003 (Update Profile).
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the profile information for a user.
    /// Supports UC003 (Update Profile).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Result with profile information if successful</returns>
    Task<Result<ProfileResponse>> GetProfileAsync(int userId);
    
    /// <summary>
    /// Updates the profile information for a user.
    /// Supports UC003 (Update Profile).
    /// </summary>
    /// <param name="request">Profile update data</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> UpdateProfileAsync(UpdateProfileRequest request);
    
    /// <summary>
    /// Verifies an email change using a token.
    /// Supports UC003 (Update Profile), Exception 3.0.E3 (Email Verification Required).
    /// </summary>
    /// <param name="request">Email verification data</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> VerifyEmailChangeAsync(VerifyEmailChangeRequest request);
}
