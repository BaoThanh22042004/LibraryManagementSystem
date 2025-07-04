using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    /// <summary>
    /// Handles authentication and account-related actions (login, registration, password reset).
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        public AccountController(IUserService userService)
        {
            _userService = userService;
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
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var dto = new LoginDto
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = false // Add to ViewModel if needed
            };
            var result = await _userService.LoginAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }
            // Set authentication cookie/session with result.User
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                new Claim(ClaimTypes.Name, result.User.FullName),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Role, result.User.Role.ToString())
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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.Role == "Member")
            {
                var dto = new RegisterMemberDto
                {
                    FullName = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Phone = model.Phone,
                    Address = model.Address,
                    PreferredMembershipNumber = model.MembershipNumber
                };
                var result = await _userService.RegisterMemberAsync(dto);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError("", result.Error);
                    return View(model);
                }
                return RedirectToAction("Login");
            }
            else
            {
                // Only Admin/Librarian can create Librarian/Admin
                var dto = new CreateUserDto
                {
                    FullName = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Role = Enum.Parse<UserRole>(model.Role),
                    Phone = model.Phone,
                    Address = model.Address
                };
                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                await _userService.CreateUserAsync(dto, currentUserId);
                return RedirectToAction("Index", "User");
            }
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
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var dto = new ForgotPasswordDto { Email = model.Email };
            var result = await _userService.ForgotPasswordAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error);
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
            var model = new ResetPasswordViewModel { Token = token, Email = "", NewPassword = "", ConfirmPassword = "" };
            return View(model);
        }

        /// <summary>
        /// Handles reset password form submission.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var dto = new ResetPasswordDto
            {
                Email = model.Email,
                ResetToken = model.Token,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            };
            var result = await _userService.ResetPasswordAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error);
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