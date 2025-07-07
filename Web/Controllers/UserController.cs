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
    /// Controller for user management operations.
    /// Implements UC001 (Create User), UC006 (Update User Info), UC007 (View User Info), UC009 (Delete User).
    /// Business Rules: BR-01, BR-03, BR-04, BR-22, BR-23, BR-24
    /// </summary>
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserController> _logger;
        private readonly CreateUserRequestValidator _createUserValidator;
        private readonly UpdateUserRequestValidator _updateUserValidator;
        private readonly UserSearchRequestValidator _searchValidator;

        public UserController(
            IUserService userService,
            IAuditService auditService,
            ILogger<UserController> logger,
            CreateUserRequestValidator createUserValidator,
            UpdateUserRequestValidator updateUserValidator,
            UserSearchRequestValidator searchValidator)
        {
            _userService = userService;
            _auditService = auditService;
            _logger = logger;
            _createUserValidator = createUserValidator;
            _updateUserValidator = updateUserValidator;
            _searchValidator = searchValidator;
        }

        /// <summary>
        /// Displays the user management index page (UC007 - View User Info).
        /// Implements BR-01 (User Role Permissions), BR-24 (RBAC)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(UserSearchRequest? searchRequest = null)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // BR-01: Check role authorization - only librarians and administrators can access user management
                var roleCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Librarian);
                if (!roleCheckResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = "Access denied. Only librarians and administrators can access user management.";
                    return RedirectToAction("Index", "Home");
                }

                searchRequest ??= new();

                var validationResult = await _searchValidator.ValidateAsync(searchRequest);
                if (!validationResult.IsValid)
                {
                    TempData["ErrorMessage"] = "Invalid search parameters. Please try again.";
                    searchRequest = new();
                }

                var result = await _userService.SearchUsersAsync(searchRequest);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View(new PagedResult<UserBasicDto>
                    {
                        Page = searchRequest.Page,
                        PageSize = searchRequest.PageSize,
                    });
                }

                ViewData["SearchTerm"] = searchRequest.SearchTerm;
                ViewData["Role"] = searchRequest.Role;
                ViewData["Status"] = searchRequest.Status;

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing user management for user {UserId}", User.GetUserId());
                TempData["ErrorMessage"] = "An error occurred while accessing user management. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Displays the user details page (UC007 - View User Info).
        /// Implements BR-03 (User Information Access), BR-24 (RBAC)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // BR-03, BR-24: Check permissions for viewing user information
                var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.View);
                if (!permissionResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = permissionResult.Error;
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userService.GetUserDetailsAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details for {TargetId}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving user details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays the create user page (UC001 - Create User).
        /// Implements BR-01 (User Role Permissions), BR-24 (RBAC)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // BR-01: Check role authorization - only librarians and admins can create users
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing user creation form for user {UserId}", User.GetUserId());
                TempData["ErrorMessage"] = "An error occurred while accessing the user creation form. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Processes the create user request (UC001 - Create User).
        /// Implements BR-01 (User Role Permissions), BR-02 (User Roles), BR-22 (Audit Logging)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // BR-01: Check role authorization - only librarians and admins can create users
                var roleCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Librarian);
                if (!roleCheckResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = "Access denied. Only librarians and administrators can create users.";
                    return RedirectToAction("Index", "Home");
                }

                // BR-01: Verify admin role for creating librarians
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

                var result = await _userService.CreateUserAsync(model);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Set ViewData for rendering
                    var adminCheckResult = await _userService.CheckUserRoleAsync(userId, UserRole.Admin);
                    ViewData["CanCreateLibrarian"] = adminCheckResult.IsSuccess;
                    
                    return View(model);
                }

                // Audit successful user creation
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Create,
                    EntityType = "User",
                    EntityId = result.Value.ToString(),
                    EntityName = model.FullName,
                    Details = $"User created successfully. Role: {model.Role}, Email: {model.Email}",
                    IsSuccess = true
                });

                _logger.LogInformation("User {CreatorId} created new user {NewUserId} with role {Role} at {Time}", 
                    userId, result.Value, model.Role, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = $"User created successfully with ID: {result.Value}";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for user {UserId}", User.GetUserId());
                TempData["ErrorMessage"] = "An error occurred while creating the user. Please try again later.";
                return View(model);
            }
        }

        /// <summary>
        /// Displays the edit user page (UC006 - Update User Info).
        /// Implements BR-01 (User Role Permissions), BR-03 (User Information Access)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.Update);
                if (!permissionResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = permissionResult.Error;
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userService.GetUserDetailsAsync(id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user form for user {TargetId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the edit form. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes the edit user request (UC006 - Update User Info).
        /// Implements BR-01 (User Role Permissions), BR-22 (Audit Logging)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateUserRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
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

                var result = await _userService.UpdateUserAsync(model);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    return View(model);
                }

                // Audit successful user update
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Update,
                    EntityType = "User",
                    EntityId = model.Id.ToString(),
                    EntityName = model.FullName,
                    Details = $"User updated successfully. Updated by: {userId}",
                    IsSuccess = true
                });

                _logger.LogInformation("User {UpdaterId} updated user {TargetId} at {Time}", 
                    userId, model.Id, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "User updated successfully";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {TargetId}", model.Id);
                TempData["ErrorMessage"] = "An error occurred while updating the user. Please try again later.";
                return View(model);
            }
        }

        /// <summary>
        /// Displays the delete user confirmation page (UC009 - Delete User).
        /// Implements BR-01 (User Role Permissions), BR-23 (Member Deletion Restriction)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var permissionResult = await _userService.CheckUserPermissionAsync(userId, id, UserAction.Delete);
                if (!permissionResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = permissionResult.Error;
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userService.GetUserDetailsAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // BR-23: For members, check if they can be deleted
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete user form for user {TargetId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the delete confirmation. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes the delete user request (UC009 - Delete User).
        /// Implements BR-01 (User Role Permissions), BR-22 (Audit Logging), BR-23 (Member Deletion Restriction)
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
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

                // Get user details for audit logging
                var userDetailsResult = await _userService.GetUserDetailsAsync(id);
                var targetUserName = userDetailsResult.IsSuccess ? userDetailsResult.Value.FullName : "Unknown";

                var result = await _userService.DeleteUserAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Audit successful user deletion
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Delete,
                    EntityType = "User",
                    EntityId = id.ToString(),
                    EntityName = targetUserName,
                    Details = $"User deleted successfully. Deleted by: {userId}",
                    IsSuccess = true
                });

                _logger.LogInformation("User {DeleterId} deleted user {TargetId} at {Time}", 
                    userId, id, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "User deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {TargetId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user. Please try again later.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
