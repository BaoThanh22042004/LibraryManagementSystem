using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Extensions;

namespace Web.Controllers
{
	/// <summary>
	/// Controller for authentication-related operations.
	/// Implements UC002 (Login), UC004 (Change Password), UC005 (Reset Password), and UC008 (Register Member).
	/// Business Rules: BR-05, BR-22, BR-24, BR-26
	/// </summary>
	public class AuthController : Controller
	{
		private readonly IAuthService _authService;
		private readonly IAuditService _auditService;
		private readonly ILogger<AuthController> _logger;
		private readonly IValidator<LoginRequest> _loginValidator;
		private readonly IValidator<RegisterRequest> _registerValidator;
		private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
		private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
		private readonly IValidator<ConfirmResetPasswordRequest> _confirmResetPasswordValidator;

		public AuthController(
			IAuthService authService,
			IAuditService auditService,
			ILogger<AuthController> logger,
			IValidator<LoginRequest> loginValidator,
			IValidator<RegisterRequest> registerValidator,
			IValidator<ChangePasswordRequest> changePasswordValidator,
			IValidator<ResetPasswordRequest> resetPasswordValidator,
			IValidator<ConfirmResetPasswordRequest> confirmResetPasswordValidator)
		{
			_authService = authService;
			_auditService = auditService;
			_logger = logger;
			_loginValidator = loginValidator;
			_registerValidator = registerValidator;
			_changePasswordValidator = changePasswordValidator;
			_resetPasswordValidator = resetPasswordValidator;
			_confirmResetPasswordValidator = confirmResetPasswordValidator;
		}

		/// <summary>
		/// Displays login page.
		/// </summary>
		[HttpGet]
		public IActionResult Login(string? returnUrl = null)
		{
			try
			{
				// If user is already authenticated, redirect to home or returnUrl
				if (User.Identity?.IsAuthenticated == true)
				{
					if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
					{
						return Redirect(returnUrl);
					}
					return RedirectToAction("Index", "Home");
				}

				ViewData["ReturnUrl"] = returnUrl;
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying login page");
				TempData["ErrorMessage"] = "An error occurred while displaying the login page. Please try again later.";
				return View();
			}
		}

		/// <summary>
		/// Processes login request (UC002 - Login).
		/// Implements BR-05 (Password Security), BR-22 (Audit Logging), BR-24 (RBAC)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
		{
			try
			{
				ViewData["ReturnUrl"] = returnUrl;

				var validationResult = await _loginValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				// BR-05: Check account lockout status
				var lockoutResult = await _authService.CheckAccountLockoutStatusAsync(model.Email);
				if (lockoutResult.IsSuccess && lockoutResult.Value.HasValue)
				{
					var lockoutEnd = lockoutResult.Value.Value;
					if (lockoutEnd > DateTime.UtcNow)
					{
						var remainingTime = lockoutEnd - DateTime.UtcNow;
						
						// Audit lockout attempt
						await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
						{
							ActionType = AuditActionType.Login,
							EntityType = "User",
							EntityId = model.Email,
							EntityName = model.Email,
							Details = $"Login attempt blocked due to account lockout. Remaining time: {Math.Ceiling(remainingTime.TotalMinutes)} minutes",
							IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
							IsSuccess = false,
							ErrorMessage = "Account temporarily locked"
						});

						ModelState.AddModelError(string.Empty, $"Account temporarily locked. Please try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
						return View(model);
					}
				}

				var result = await _authService.LoginAsync(model);
				if (!result.IsSuccess)
				{
					await _authService.RecordFailedLoginAttemptAsync(model.Email);
					ModelState.AddModelError(string.Empty, "Invalid email or password");
					return View(model);
				}

				await _authService.ClearFailedLoginAttemptsAsync(result.Value.UserId);

				// BR-24: Create claims for role-based access control
				var claims = new List<Claim>
				{
					new(ClaimTypes.NameIdentifier, result.Value.UserId.ToString()),
					new(ClaimTypes.Name, result.Value.FullName),
					new(ClaimTypes.Email, result.Value.Email),
					new(ClaimTypes.Role, result.Value.Role.ToString())
				};

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var authProperties = new AuthenticationProperties
				{
					IsPersistent = model.RememberMe,
					ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(2)
				};

				await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

				return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
					? Redirect(returnUrl)
					: RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing login request for {Email}", model.Email);
				TempData["ErrorMessage"] = "An error occurred during login. Please try again later.";
				return View(model);
			}
		}

		/// <summary>
		/// Logs out the user.
		/// Implements BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			try
			{
				await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during logout");
				TempData["ErrorMessage"] = "An error occurred during logout. Please try again later.";
				return RedirectToAction("Index", "Home");
			}
		}

		/// <summary>
		/// Displays registration page (UC008 - Register Member).
		/// </summary>
		[HttpGet]
		public IActionResult Register()
		{
			try
			{
				// If user is already authenticated, redirect to home
				if (User.Identity?.IsAuthenticated == true)
				{
					return RedirectToAction("Index", "Home");
				}

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying registration page");
				TempData["ErrorMessage"] = "An error occurred while displaying the registration page. Please try again later.";
				return View();
			}
		}

		/// <summary>
		/// Processes registration request (UC008 - Register Member).
		/// Implements BR-02 (User Roles), BR-05 (Password Security), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterRequest model)
		{
			try
			{
				// If user is already authenticated, redirect to home
				if (User.Identity?.IsAuthenticated == true)
				{
					return RedirectToAction("Index", "Home");
				}

				var validationResult = await _registerValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				var result = await _authService.RegisterMemberAsync(model);

				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				// Audit successful registration
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					ActionType = AuditActionType.Register,
					EntityType = "Member",
					EntityId = result.Value.ToString(),
					EntityName = model.FullName,
					Details = $"Member registered successfully. Email: {model.Email}, PreferredMembershipNumber: {model.PreferredMembershipNumber}",
					IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
				});

				_logger.LogInformation("Member registered successfully: {UserId} - {Email}", 
					result.Value, model.Email);

				// Redirect to login page with success message
				TempData["SuccessMessage"] = "Registration successful! Please log in with your new account.";
				return RedirectToAction(nameof(Login));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing registration request for {Email}", model.Email);
				TempData["ErrorMessage"] = "An error occurred during registration. Please try again later.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays forgot password page (UC005 - Reset Password).
		/// </summary>
		[HttpGet]
		public IActionResult ForgotPassword()
		{
			try
			{
				if (User.Identity?.IsAuthenticated == true)
				{
					return RedirectToAction("Index", "Home");
				}

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying forgot password page");
				TempData["ErrorMessage"] = "An error occurred while displaying the forgot password page. Please try again later.";
				return View();
			}
		}

		/// <summary>
		/// Processes forgot password request (UC005 - Reset Password).
		/// Implements BR-22 (Audit Logging), BR-26 (Password Reset Rate Limiting)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(ResetPasswordRequest model)
		{
			try
			{
				var validationResult = await _resetPasswordValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				// Always return success view to prevent email enumeration attacks
				var result = await _authService.RequestPasswordResetAsync(model);
				
				// Audit password reset request (success status based on actual result)
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					ActionType = AuditActionType.PasswordReset,
					EntityType = "User",
					EntityId = model.Email,
					EntityName = model.Email,
					Details = result.IsSuccess ? "Password reset email sent successfully" : "Password reset requested for non-existent email",
					IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
					IsSuccess = result.IsSuccess,
					ErrorMessage = result.IsSuccess ? null : result.Error
				});

				_logger.LogInformation("Password reset requested for {Email} at {Time}", model.Email, DateTime.UtcNow);

				return View("ForgotPasswordConfirmation");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing forgot password request for {Email}", model.Email);
				TempData["ErrorMessage"] = "An error occurred while processing your request. Please try again later.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays reset password page (UC005 - Reset Password).
		/// </summary>
		[HttpGet]
		public IActionResult ResetPassword(string email, string token)
		{
			try
			{
				if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
				{
					return BadRequest("Invalid password reset token");
				}

				var model = new ConfirmResetPasswordRequest
				{
					Email = email,
					Token = token
				};

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying reset password page for {Email}", email);
				TempData["ErrorMessage"] = "An error occurred while displaying the reset password page. Please try again later.";
				return RedirectToAction(nameof(ForgotPassword));
			}
		}

		/// <summary>
		/// Processes reset password request (UC005 - Reset Password).
		/// Implements BR-05 (Password Security), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ConfirmResetPasswordRequest model)
		{
			try
			{
				var validationResult = await _confirmResetPasswordValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				var result = await _authService.ConfirmPasswordResetAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				// Audit successful password reset
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					ActionType = AuditActionType.PasswordReset,
					EntityType = "User",
					EntityId = model.Email,
					EntityName = model.Email,
					Details = "Password reset completed successfully",
					IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
				});

				_logger.LogInformation("Password reset completed successfully for {Email} at {Time}", model.Email, DateTime.UtcNow);

				return View("ResetPasswordConfirmation");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing reset password request for {Email}", model.Email);
				TempData["ErrorMessage"] = "An error occurred while resetting your password. Please try again later.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays change password page (UC004 - Change Password).
		/// </summary>
		[HttpGet]
		public IActionResult ChangePassword()
		{
			try
			{
				// Ensure user is authenticated
				if (User.Identity?.IsAuthenticated != true)
				{
					return RedirectToAction(nameof(Login));
				}

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying change password page");
				TempData["ErrorMessage"] = "An error occurred while displaying the change password page. Please try again later.";
				return RedirectToAction("Index", "Home");
			}
		}

		/// <summary>
		/// Processes change password request (UC004 - Change Password).
		/// Implements BR-05 (Password Security), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordRequest model)
		{
			try
			{
				// Ensure user is authenticated
				if (User.Identity?.IsAuthenticated != true)
				{
					return RedirectToAction(nameof(Login));
				}

				// Set user ID from claims
				if (User.TryGetUserId(out int userId))
				{
					model.UserId = userId;
				}
				else
				{
					return RedirectToAction(nameof(Login));
				}

				var validationResult = await _changePasswordValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				var result = await _authService.ChangePasswordAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				// Audit successful password change
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.PasswordChange,
					EntityType = "User",
					EntityId = userId.ToString(),
					EntityName = User.Identity.Name ?? "Unknown",
					Details = "Password changed successfully",
				});

				_logger.LogInformation("Password changed successfully for user {UserId} at {Time}", userId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Your password has been changed successfully.";
				return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing change password request");
				TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again later.";
				return View(model);
			}
		}
	}
}
