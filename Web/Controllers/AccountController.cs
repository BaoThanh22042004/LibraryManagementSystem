using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Application.DTOs;
using Application.Common;
using MediatR;
using Application.Features.Users.Commands;
using Domain.Enums;

namespace Web.Controllers
{
	/// <summary>
	/// Handles authentication and account-related actions (login, registration, password reset).
	/// </summary>
	public class AccountController : Controller
	{
		private readonly IMediator _mediator;

		public AccountController(IMediator mediator)
		{
			_mediator = mediator;
		}

		/// <summary>
		/// Displays the login form.
		/// </summary>
		[AllowAnonymous]
		public IActionResult Login() => View();

		/// <summary>
		/// Handles login form submission.
		/// </summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Login(LoginDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var response = await _mediator.Send(new LoginCommand(model));
			if (!response.IsSuccess || response.User == null)
			{
				ModelState.AddModelError("", response.Message ?? "Login failed.");
				return View(model);
			}
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
				new Claim(ClaimTypes.Name, response.User.FullName),
				new Claim(ClaimTypes.Email, response.User.Email),
				new Claim(ClaimTypes.Role, response.User.Role.ToString())
			};
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
			return RedirectToAction("Index", "Home");
		}

		/// <summary>
		/// Displays the registration form.
		/// </summary>
		[AllowAnonymous]
		public IActionResult Register() => View();

		/// <summary>
		/// Handles registration form submission.
		/// </summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Register(RegisterMemberDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var result = await _mediator.Send(new RegisterMemberCommand(model));
			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.Error ?? "Registration failed.");
				return View(model);
			}
			return RedirectToAction("Login");
		}

		/// <summary>
		/// Displays the forgot password form.
		/// </summary>
		[AllowAnonymous]
		public IActionResult ForgotPassword() => View();

		/// <summary>
		/// Handles forgot password form submission.
		/// </summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var result = await _mediator.Send(new ForgotPasswordCommand(model));
			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.Error ?? "Failed to send reset link.");
				return View(model);
			}
			return RedirectToAction("Login");
		}

		/// <summary>
		/// Displays the reset password form.
		/// </summary>
		[AllowAnonymous]
		public IActionResult ResetPassword(string token)
		{
			var model = new ResetPasswordDto { ResetToken = token };
			return View(model);
		}

		/// <summary>
		/// Handles reset password form submission.
		/// </summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
		{
			if (!ModelState.IsValid) return View(model);
			var result = await _mediator.Send(new ResetPasswordCommand(model));
			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.Error ?? "Failed to reset password.");
				return View(model);
			}
			return RedirectToAction("Login");
		}

		[Authorize]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login");
		}

		[AllowAnonymous]
		public IActionResult AccessDenied() => View();
	}
}