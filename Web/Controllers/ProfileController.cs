using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Application.DTOs;
using Application.Common;
using MediatR;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;

namespace Web.Controllers
{
    /// <summary>
    /// Handles profile management for the currently logged-in user.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Displays the current user's profile.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var user = await _mediator.Send(new GetUserByIdQuery(currentUserId));
            if (user == null) return NotFound();
            var model = new UpdateProfileDto
            {
                FullName = user.FullName,
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
            var user = await _mediator.Send(new GetUserByIdQuery(currentUserId));
            if (user == null) return NotFound();
            var model = new UpdateProfileDto
			{
                FullName = user.FullName,
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
        public async Task<IActionResult> Edit(UpdateProfileDto model)
        {
            if (!ModelState.IsValid) return View(model);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var result = await _mediator.Send(new UpdateProfileCommand(currentUserId, model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to update profile.");
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
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!ModelState.IsValid) return View(model);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            var result = await _mediator.Send(new ChangePasswordCommand(currentUserId, model));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to change password.");
                return View(model);
            }
            return RedirectToAction("Index");
        }
    }
} 