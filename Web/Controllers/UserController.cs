using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;
using Application.Interfaces.Services;
using Application.DTOs;
using Application.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Web.Controllers
{
    /// <summary>
    /// Handles user management actions for Admin and Librarian roles.
    /// </summary>
    [Authorize(Roles = "Admin,Librarian")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Displays a list of users.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var pagedResult = await _userService.GetPaginatedUsersAsync(new PagedRequest { PageNumber = 1, PageSize = 100 });
            var model = pagedResult.Items.Select(u => new UserViewModel
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                Phone = u.Phone,
                Address = u.Address,
                MembershipStatus = u is UserDetailsDto details ? details.Member?.MembershipStatus.ToString() ?? "" : "",
                MembershipNumber = u is UserDetailsDto details2 ? details2.Member?.MembershipNumber ?? "" : "",
                Password = "" // Not displayed in list view
            }).ToList();
            return View(model);
        }

        /// <summary>
        /// Displays details for a specific user.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            var details = user as UserDetailsDto;
            var model = new UserViewModel
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Phone = user.Phone,
                Address = user.Address,
                MembershipStatus = details?.Member?.MembershipStatus.ToString() ?? "",
                MembershipNumber = details?.Member?.MembershipNumber ?? "",
                Password = "" // Not displayed in details view
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
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var dto = new CreateUserDto
            {
                FullName = model.Name,
                Email = model.Email,
                Password = model.Password, // Add Password to UserViewModel and View
                Role = Enum.Parse<UserRole>(model.Role)
            };
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized();
            await _userService.CreateUserAsync(dto, currentUserId);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays the edit user form.
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            var details = user as UserDetailsDto;
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Phone = user.Phone,
                Address = user.Address,
                MembershipStatus = details?.Member?.MembershipStatus.ToString() ?? "",
                MembershipNumber = details?.Member?.MembershipNumber ?? ""
            };
            return View(model);
        }

        /// <summary>
        /// Handles edit user form submission.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var dto = new UpdateUserDto
            {
                FullName = model.Name,
                Email = model.Email,
                Role = Enum.Parse<UserRole>(model.Role)
            };
            await _userService.UpdateUserAsync(model.Id, dto);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays the delete user confirmation.
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            var details = user as UserDetailsDto;
            var model = new UserViewModel
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Phone = user.Phone,
                Address = user.Address,
                MembershipStatus = details?.Member?.MembershipStatus.ToString() ?? "",
                MembershipNumber = details?.Member?.MembershipNumber ?? "",
                Password = "" // Not displayed in details view
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
            await _userService.DeleteUserAsync(id, currentUserId);
            return RedirectToAction("Index");
        }
    }
} 