using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
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
    /// </summary>
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly LoginRequestValidator _loginValidator;
        private readonly RegisterRequestValidator _registerValidator;
        private readonly ChangePasswordRequestValidator _changePasswordValidator;
        private readonly ResetPasswordRequestValidator _resetPasswordValidator;
        private readonly ConfirmResetPasswordRequestValidator _confirmResetPasswordValidator;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            LoginRequestValidator loginValidator,
            RegisterRequestValidator registerValidator,
            ChangePasswordRequestValidator changePasswordValidator,
            ResetPasswordRequestValidator resetPasswordValidator,
            ConfirmResetPasswordRequestValidator confirmResetPasswordValidator)
        {
            _authService = authService;
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

        /// <summary>
        /// Processes login request (UC002 - Login).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var validationResult = await _loginValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState);
                return View(model);
            }

            // Check if account is locked
            var lockoutResult = await _authService.CheckAccountLockoutStatusAsync(model.Email);
            if (lockoutResult.IsSuccess && lockoutResult.Value.HasValue)
            {
                DateTime lockoutEnd = lockoutResult.Value.Value;
                if (lockoutEnd > DateTime.UtcNow)
                {
                    TimeSpan remainingTime = lockoutEnd - DateTime.UtcNow;
                    ModelState.AddModelError(string.Empty, $"Account temporarily locked. Please try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
                    return View(model);
                }
            }

            var result = await _authService.LoginAsync(model);

            if (!result.IsSuccess)
            {
                // Record failed login attempt
                await _authService.RecordFailedLoginAttemptAsync(model.Email);
                
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            // Clear failed login attempts
            await _authService.ClearFailedLoginAttemptsAsync(result.Value.UserId);

            // Create claims for cookie auth
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.Value.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.Value.FullName),
                new Claim(ClaimTypes.Email, result.Value.Email),
                new Claim(ClaimTypes.Role, result.Value.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {UserId} logged in at {Time}", result.Value.UserId, DateTime.UtcNow);

            // If password change is required, redirect to change password page
            if (result.Value.RequirePasswordChange)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Logs out the user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out at {Time}", DateTime.UtcNow);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Displays registration page (UC008 - Register Member).
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // If user is already authenticated, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        /// <summary>
        /// Processes registration request (UC008 - Register Member).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest model)
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

            _logger.LogInformation("New member registered with ID {UserId} at {Time}", result.Value, DateTime.UtcNow);
            
            // Redirect to login page with success message
            TempData["SuccessMessage"] = "Registration successful! Please log in with your new account.";
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Displays forgot password page (UC005 - Reset Password).
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
			}

			return View();
        }

        /// <summary>
        /// Processes forgot password request (UC005 - Reset Password).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ResetPasswordRequest model)
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState);
                return View(model);
            }

            // Always return success view to prevent email enumeration attacks
            await _authService.RequestPasswordResetAsync(model);
            
            return View("ForgotPasswordConfirmation");
        }

        /// <summary>
        /// Displays reset password page (UC005 - Reset Password).
        /// </summary>
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
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

        /// <summary>
        /// Processes reset password request (UC005 - Reset Password).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ConfirmResetPasswordRequest model)
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

            _logger.LogInformation("Password reset completed for email {Email} at {Time}", model.Email, DateTime.UtcNow);
            
            return View("ResetPasswordConfirmation");
        }

        /// <summary>
        /// Displays change password page (UC004 - Change Password).
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Ensure user is authenticated
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        /// <summary>
        /// Processes change password request (UC004 - Change Password).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest model)
        {
            // Ensure user is authenticated
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login));
            }

            // Set user ID from claims
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
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

            _logger.LogInformation("Password changed for user {UserId} at {Time}", userId, DateTime.UtcNow);
            
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}
