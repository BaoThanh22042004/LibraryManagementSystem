using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces;
using Web.Extensions;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuditService _auditService;

    public HomeController(ILogger<HomeController> logger, IAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
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
