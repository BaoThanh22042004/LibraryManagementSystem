using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Extensions;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for user profile operations.
    /// Implements UC003 (Update Profile).
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;
        private readonly UpdateProfileRequestValidator _updateProfileValidator;

        public ProfileController(
            IProfileService profileService,
            ILogger<ProfileController> logger,
            UpdateProfileRequestValidator updateProfileValidator)
        {
            _profileService = profileService;
            _logger = logger;
            _updateProfileValidator = updateProfileValidator;
        }

        /// <summary>
        /// Displays the user's profile page (UC003 - Update Profile).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
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

            return View(result.Value);
        }

        /// <summary>
        /// Displays the edit profile form (UC003 - Update Profile).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
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

        /// <summary>
        /// Processes the profile update request (UC003 - Update Profile).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProfileRequest model)
        {
            if (!User.TryGetUserId(out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Ensure the user ID in the model matches the authenticated user
            if (model.UserId != userId)
            {
                ModelState.AddModelError(string.Empty, "Invalid user ID");
                return View(model);
            }

            var validationResult = await _updateProfileValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState);
                return View(model);
            }

            var result = await _profileService.UpdateProfileAsync(model);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            _logger.LogInformation("Profile updated for user {UserId} at {Time}", userId, DateTime.UtcNow);
            
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            if (model.Email != User.FindFirstValue(ClaimTypes.Email))
            {
                TempData["InfoMessage"] = "A verification email has been sent to your new email address. Please check your inbox and click the verification link to complete the email change.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Verifies an email change using a token (UC003 - Update Profile).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(string email, string token)
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

            TempData["SuccessMessage"] = "Your email address has been verified successfully.";
            
            // If user is logged in, redirect to profile; otherwise, to login
            return User.Identity?.IsAuthenticated == true
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Login", "Auth");
        }
    }
}
