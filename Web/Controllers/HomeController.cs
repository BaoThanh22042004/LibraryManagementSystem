using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web.Extensions;
using Web.Models;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuditService _auditService;
    private readonly IBookService _bookService;
    private readonly ICategoryService _categoryService;

    public HomeController(
        ILogger<HomeController> logger,
        IAuditService auditService,
        IBookService bookService,
        ICategoryService categoryService)
    {
        _logger = logger;
        _auditService = auditService;
        _bookService = bookService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId = null, string? status = null, string? search = null, int page = 1, int pageSize = 12)
    {
        try
        {
            // Get categories for filter dropdown
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categoriesResult.IsSuccess ? categoriesResult.Value : new List<CategoryDto>();
            ViewBag.SelectedCategoryId = categoryId;

            // Prepare book search request
            var searchRequest = new BookSearchRequest
            {
                Page = page,
                PageSize = pageSize,
                CategoryId = categoryId,
                SearchTerm = search
            };

            // Add status filter if provided and valid
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookStatus>(status, out var parsedStatus))
            {
                // Add this property to BookSearchRequest if not present
                var statusProp = searchRequest.GetType().GetProperty("Status");
                if (statusProp != null)
                {
                    statusProp.SetValue(searchRequest, parsedStatus);
                }
            }

            // Get books (filtered by category, status, and title if provided)
            var booksResult = await _bookService.SearchBooksAsync(searchRequest);

            // Return paged result to view
            if (booksResult.IsSuccess && booksResult.Value != null)
            {
                return View(booksResult.Value);
            }
            else
            {
                return View(new PagedResult<BookBasicDto>(Array.Empty<BookBasicDto>(), 0, page, pageSize));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page book list");
            TempData["ErrorMessage"] = "Unable to load books. Please try again later.";
            ViewBag.Categories = new List<CategoryDto>();
            return View(new PagedResult<BookBasicDto>(Array.Empty<BookBasicDto>(), 0, page, pageSize));
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var result = await _auditService.GetDashboardStatsAsync();
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Error;
                return View("Dashboard", null);
            }
            // Audit log
            if (User.TryGetUserId(out int staffId))
            {
                await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
                {
                    UserId = staffId,
                    ActionType = Domain.Enums.AuditActionType.Read,
                    EntityType = "Dashboard",
                    Details = "Viewed dashboard statistics",
                    IsSuccess = true
                });
            }
            return View("Dashboard", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            TempData["ErrorMessage"] = "Unable to load dashboard statistics.";
            return View("Dashboard", null);
        }
    }
}