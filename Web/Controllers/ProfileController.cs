using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Extensions;

namespace Web.Controllers
{
	/// <summary>
	/// Controller for user profile operations.
	/// Implements UC003 (Update Profile) and UC004 (Change Password).
	/// Business Rules: BR-03 (User Information Access), BR-04 (User Data Privacy), BR-05 (Password Security), BR-22 (Audit Logging)
	/// </summary>
	[Authorize]
	public class ProfileController : Controller
	{
		private readonly IProfileService _profileService;
		private readonly IAuditService _auditService;
		private readonly ILogger<ProfileController> _logger;
		private readonly UpdateProfileRequestValidator _updateProfileValidator;

		public ProfileController(
			IProfileService profileService,
			IAuditService auditService,
			ILogger<ProfileController> logger,
			UpdateProfileRequestValidator updateProfileValidator)
		{
			_profileService = profileService;
			_auditService = auditService;
			_logger = logger;
			_updateProfileValidator = updateProfileValidator;
		}

		/// <summary>
		/// Displays the user's profile page (UC003 - Update Profile).
		/// Implements BR-03 (User Information Access)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var result = await _profileService.GetProfileAsync(userId);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Home");
				}

				// Audit successful profile access
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Read,
					EntityType = "UserProfile",
					EntityId = userId.ToString(),
					EntityName = result.Value.FullName,
					Details = "User viewed their profile",
					IsSuccess = true
				});

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving profile for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while retrieving your profile.";
				return RedirectToAction("Index", "Home");
			}
		}

		/// <summary>
		/// Displays the edit profile form (UC003 - Update Profile).
		/// Implements BR-03 (User Information Access)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Edit()
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var result = await _profileService.GetProfileAsync(userId);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Home");
				}

				var updateProfileRequest = new UpdateProfileRequest
				{
					UserId = userId,
					FullName = result.Value.FullName,
					Email = result.Value.Email,
					Phone = result.Value.Phone,
					Address = result.Value.Address
				};

				return View(updateProfileRequest);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading edit profile form for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while loading the edit form.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Processes the profile update request (UC003 - Update Profile).
		/// Implements BR-03 (User Information Access), BR-04 (User Data Privacy), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(UpdateProfileRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				// BR-03: Ensure the user ID in the model matches the authenticated user
				if (model.UserId != userId)
				{
					ModelState.AddModelError(string.Empty, "Invalid user ID");
					await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
					{
						UserId = userId,
						ActionType = AuditActionType.Update,
						EntityType = "UserProfile",
						EntityId = model.UserId.ToString(),
						EntityName = model.FullName,
						Details = "Attempted to update profile with invalid user ID.",
						IsSuccess = false,
						ErrorMessage = "Invalid user ID"
					});
					return View(model);
				}

				var validationResult = await _updateProfileValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
					{
						UserId = userId,
						ActionType = AuditActionType.Update,
						EntityType = "UserProfile",
						EntityId = model.UserId.ToString(),
						EntityName = model.FullName,
						Details = "Profile update validation failed: " + string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
						IsSuccess = false,
						ErrorMessage = "Validation failed"
					});
					return View(model);
				}

				var result = await _profileService.UpdateProfileAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
					{
						UserId = userId,
						ActionType = AuditActionType.Update,
						EntityType = "UserProfile",
						EntityId = model.UserId.ToString(),
						EntityName = model.FullName,
						Details = "Profile update failed: " + result.Error,
						IsSuccess = false,
						ErrorMessage = result.Error
					});
					return View(model);
				}

				// Audit successful profile update
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Update,
					EntityType = "UserProfile",
					EntityId = userId.ToString(),
					EntityName = model.FullName,
					Details = $"Profile updated successfully. Email: {model.Email}",
					IsSuccess = true
				});

				_logger.LogInformation("Profile updated successfully for user {UserId} at {Time}", userId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Your profile has been updated successfully.";
				if (model.Email != User.FindFirstValue(ClaimTypes.Email))
				{
					TempData["InfoMessage"] = "A verification email has been sent to your new email address. Please check your inbox and click the verification link to complete the email change.";
				}

				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating profile for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while updating your profile.";
				return View(model);
			}
		}

		/// <summary>
		/// Verifies an email change using a token (UC003 - Update Profile).
		/// Implements BR-04 (User Data Privacy), BR-22 (Audit Logging)
		/// </summary>
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> VerifyEmail(string email, string token)
		{
			try
			{
				if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
				{
					return BadRequest("Invalid email verification token");
				}

				var request = new VerifyEmailChangeRequest
				{
					Email = email,
					Token = token
				};

				var result = await _profileService.VerifyEmailChangeAsync(request);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Home");
				}

				// Audit successful email verification
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					ActionType = AuditActionType.Update,
					EntityType = "UserProfile",
					EntityName = email,
					Details = $"Email verified successfully: {email}",
					IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
				});

				_logger.LogInformation("Email verified successfully for {Email} at {Time}", email, DateTime.UtcNow);
				TempData["SuccessMessage"] = "Your email address has been verified successfully.";

				// If user is logged in, redirect to profile; otherwise, to login
				return User.Identity?.IsAuthenticated == true
					? RedirectToAction(nameof(Index))
					: RedirectToAction("Login", "Auth");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error verifying email {Email}", email);
				TempData["ErrorMessage"] = "An error occurred while verifying your email address.";
				return RedirectToAction("Index", "Home");
			}
		}
	}
}
