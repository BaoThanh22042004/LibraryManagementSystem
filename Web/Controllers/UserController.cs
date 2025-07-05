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
    /// Controller for user management operations.
    /// Implements UC001 (Create User), UC006 (Update User Info), UC007 (View User Info), UC009 (Delete User).
    /// </summary>
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly CreateUserRequestValidator _createUserValidator;
        private readonly UpdateUserRequestValidator _updateUserValidator;
        private readonly UserSearchRequestValidator _searchValidator;

        public UserController(
            IUserService userService,
            ILogger<UserController> logger,
            CreateUserRequestValidator createUserValidator,
            UpdateUserRequestValidator updateUserValidator,
            UserSearchRequestValidator searchValidator)
        {
            _userService = userService;
            _logger = logger;
            _createUserValidator = createUserValidator;
            _updateUserValidator = updateUserValidator;
            _searchValidator = searchValidator;
        }

        /// <summary>
        /// Displays the user management index page (UC007 - View User Info).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(UserSearchRequest? searchRequest = null)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check role authorization
            var roleCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Librarian);
            if (!roleCheckResult.IsSuccess)
            {
                TempData["ErrorMessage"] = "Access denied. Only librarians and administrators can access user management.";
                return RedirectToAction("Index", "Home");
            }

            searchRequest ??= new UserSearchRequest { PageNumber = 1, PageSize = 10 };

            var validationResult = await _searchValidator.ValidateAsync(searchRequest);
            if (!validationResult.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid search parameters. Please try again.";
                searchRequest = new UserSearchRequest { PageNumber = 1, PageSize = 10 };
            }

            var result = await _userService.SearchUsersAsync(searchRequest, userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return View(new UserSearchResponse
                {
                    Items = [],
                    Page = searchRequest.PageNumber,
                    PageSize = searchRequest.PageSize,
                    Count = 0
                });
            }

            ViewData["SearchTerm"] = searchRequest.SearchTerm;
            ViewData["Role"] = searchRequest.Role;
            ViewData["Status"] = searchRequest.Status;

            return View(result.Value);
        }

        /// <summary>
        /// Displays the user details page (UC007 - View User Info).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.View);
            if (!permissionResult.IsSuccess)
            {
                TempData["ErrorMessage"] = permissionResult.Error;
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.GetUserDetailsAsync(id, userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }

        /// <summary>
        /// Displays the create user page (UC001 - Create User).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check role authorization - only librarians and admins can create users
            var roleCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Librarian);
            if (!roleCheckResult.IsSuccess)
            {
                TempData["ErrorMessage"] = "Access denied. Only librarians and administrators can create users.";
                return RedirectToAction("Index", "Home");
            }

            // Determine if user can create librarians (admin only) or only members
            var adminCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Admin);
            ViewData["CanCreateLibrarian"] = adminCheckResult.IsSuccess;

            return View();
        }

        /// <summary>
        /// Processes the create user request (UC001 - Create User).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserRequest model)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check role authorization - only librarians and admins can create users
            var roleCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Librarian);
            if (!roleCheckResult.IsSuccess)
            {
                TempData["ErrorMessage"] = "Access denied. Only librarians and administrators can create users.";
                return RedirectToAction("Index", "Home");
            }

            // Verify admin role for creating librarians
            if (model.Role != UserRole.Member)
            {
                var adminCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Admin);
                if (!adminCheckResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Access denied. Only administrators can create librarian accounts.");
                    
                    // Set ViewData for rendering
                    ViewData["CanCreateLibrarian"] = false;
                    return View(model);
                }
            }

            var validationResult = await _createUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState);
                
                // Set ViewData for rendering
                var adminCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Admin);
                ViewData["CanCreateLibrarian"] = adminCheckResult.IsSuccess;
                
                return View(model);
            }

            var result = await _userService.CreateUserAsync(model, userId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                
                // Set ViewData for rendering
                var adminCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Admin);
                ViewData["CanCreateLibrarian"] = adminCheckResult.IsSuccess;
                
                return View(model);
            }

            _logger.LogInformation("User {CreatorId} created new user {UserId} with role {Role} at {Time}", 
                userId, result.Value, model.Role, DateTime.UtcNow);
            
            TempData["SuccessMessage"] = $"User created successfully with ID: {result.Value}";
            return RedirectToAction(nameof(Details), new { id = result.Value });
        }

        /// <summary>
        /// Displays the edit user page (UC006 - Update User Info).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.Update);
            if (!permissionResult.IsSuccess)
            {
                TempData["ErrorMessage"] = permissionResult.Error;
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.GetUserDetailsAsync(id, userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            var updateUserRequest = new UpdateUserRequest
            {
                Id = result.Value.Id,
                FullName = result.Value.FullName,
                Email = result.Value.Email,
                Phone = result.Value.Phone,
                Address = result.Value.Address,
                Status = result.Value.Status
            };

            return View(updateUserRequest);
        }

        /// <summary>
        /// Processes the edit user request (UC006 - Update User Info).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateUserRequest model)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionResult = await _userService.CheckUserPermissionAsync(userId, model.Id, UserAction.Update);
            if (!permissionResult.IsSuccess)
            {
                TempData["ErrorMessage"] = permissionResult.Error;
                return RedirectToAction(nameof(Index));
            }

            var validationResult = await _updateUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState);
                return View(model);
            }

            var result = await _userService.UpdateUserAsync(model, userId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            _logger.LogInformation("User {UpdaterId} updated user {UserId} at {Time}", 
                userId, model.Id, DateTime.UtcNow);
            
            TempData["SuccessMessage"] = "User updated successfully";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        /// <summary>
        /// Displays the delete user confirmation page (UC009 - Delete User).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.Delete);
            if (!permissionResult.IsSuccess)
            {
                TempData["ErrorMessage"] = permissionResult.Error;
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.GetUserDetailsAsync(id, userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            // For members, check if they can be deleted
            if (result.Value.Role == UserRole.Member && result.Value.MemberDetails != null)
            {
                var memberValidationResult = await _userService.CanDeleteMemberAsync(result.Value.MemberDetails.Id);
                if (memberValidationResult.IsSuccess && !memberValidationResult.Value.CanDelete)
                {
                    ViewData["CanDelete"] = false;
                    ViewData["DeletionBlockedReason"] = memberValidationResult.Value.Message;
                }
                else
                {
                    ViewData["CanDelete"] = true;
                }
            }
            else
            {
                ViewData["CanDelete"] = true;
            }

            return View(result.Value);
        }

        /// <summary>
        /// Processes the delete user request (UC009 - Delete User).
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.Delete);
            if (!permissionResult.IsSuccess)
            {
                TempData["ErrorMessage"] = permissionResult.Error;
                return RedirectToAction(nameof(Index));
            }

            // Check if user is trying to delete themselves
            if (id == userId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _userService.DeleteUserAsync(id, userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            _logger.LogInformation("User {DeleterId} deleted user {UserId} at {Time}", 
                userId, id, DateTime.UtcNow);
            
            TempData["SuccessMessage"] = "User deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
