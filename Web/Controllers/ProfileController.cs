using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;
using Application.Interfaces.Services;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Web.Controllers
{
    /// <summary>
    /// Handles profile management for the currently logged-in user.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Displays the current user's profile.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var user = await _userService.GetUserByIdAsync(currentUserId);
            if (user == null) return NotFound();
            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            };
            return View(model);
        }

        /// <summary>
        /// Displays the edit profile form.
        /// </summary>
        public async Task<IActionResult> Edit()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var user = await _userService.GetUserByIdAsync(currentUserId);
            if (user == null) return NotFound();
            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            };
            return View(model);
        }

        /// <summary>
        /// Handles edit profile form submission.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var dto = new UpdateProfileDto
            {
                FullName = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address
            };
            var result = await _userService.UpdateProfileAsync(currentUserId, dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error);
                return View(model);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays the change password form.
        /// </summary>
        public IActionResult ChangePassword() => View();

        /// <summary>
        /// Handles change password form submission.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var dto = new ChangePasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmPassword
            };
            try
            {
                await _userService.ChangePasswordAsync(currentUserId, dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            return RedirectToAction("Index");
        }
    }
} 