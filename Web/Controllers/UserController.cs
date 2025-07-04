using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;

namespace Web.Controllers
{
	/// <summary>
	/// Handles user management actions for Admin and Librarian roles.
	/// </summary>
	[Authorize(Roles = "Admin,Librarian")]
	public class UserController : Controller
	{
		private readonly IMediator _mediator;

		public UserController(IMediator mediator)
		{
			_mediator = mediator;
		}

		/// <summary>
		/// Displays a list of users.
		/// </summary>
		public async Task<IActionResult> Index()
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
			int.TryParse(userIdStr, out int currentUserId);
			var pagedResult = await _mediator.Send(new GetUsersQuery(1, 100, null, currentUserId));
			var model = pagedResult.Items.Select(u => new UserDto
			{
				Id = u.Id,
				FullName = u.FullName,
				Email = u.Email,
				Role = u.Role,
				Status = u.Status,
				Phone = u.Phone,
				Address = u.Address,

			}).ToList();
			return View(model);
		}

		/// <summary>
		/// Displays details for a specific user.
		/// </summary>
		public async Task<IActionResult> Details(int id)
		{
			var user = await _mediator.Send(new GetUserByIdQuery(id));
			if (user == null) return NotFound();
			var model = new UserDto
			{
				Id = user.Id,
				FullName = user.FullName,
				Email = user.Email,
				Role = user.Role,
				Status = user.Status,
				Phone = user.Phone,
				Address = user.Address,
			};
			return View(model);
		}

		/// <summary>
		/// Displays the create user form.
		/// </summary>
		public IActionResult Create() => View();

		/// <summary>
		/// Handles create user form submission.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Create(CreateUserDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdStr, out int currentUserId))
				return Unauthorized();
			var result = await _mediator.Send(new CreateUserCommand(model, currentUserId));
			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.Error ?? "Failed to create user.");
				return View(model);
			}
			return RedirectToAction("Index");
		}

		/// <summary>
		/// Displays the edit user form.
		/// </summary>
		public async Task<IActionResult> Edit(int id)
		{
			var user = await _mediator.Send(new GetUserByIdQuery(id));
			if (user == null) return NotFound();
			var model = new UserDto
			{
				Id = user.Id,
				FullName = user.FullName,
				Email = user.Email,
				Role = user.Role,
				Status = user.Status,
				Phone = user.Phone,
				Address = user.Address,
			};
			return View(model);
		}

		/// <summary>
		/// Handles edit user form submission.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Edit(int id, UpdateUserDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var result = await _mediator.Send(new UpdateUserCommand(id, model));
			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.Error ?? "Failed to update user.");
				return View(model);
			}
			return RedirectToAction("Index");
		}

		/// <summary>
		/// Displays the delete user confirmation.
		/// </summary>
		public async Task<IActionResult> Delete(int id)
		{
			var user = await _mediator.Send(new GetUserByIdQuery(id));
			if (user == null) return NotFound();
			var model = new UserDto
			{
				Id = user.Id,
				FullName = user.FullName,
				Email = user.Email,
				Role = user.Role,
				Status = user.Status,
				Phone = user.Phone,
				Address = user.Address,
			};
			return View(model);
		}

		/// <summary>
		/// Handles delete user confirmation.
		/// </summary>
		[HttpPost, ActionName("Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdStr, out int currentUserId))
				return Unauthorized();
			var result = await _mediator.Send(new DeleteUserCommand(id, currentUserId));
			if (!result.IsSuccess)
			{
				TempData["Error"] = result.Error ?? "Failed to delete user.";
				return RedirectToAction("Delete", new { id });
			}
			return RedirectToAction("Index");
		}
	}
}