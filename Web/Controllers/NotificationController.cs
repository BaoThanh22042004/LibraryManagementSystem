using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;
using Web.Extensions;

namespace Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    //private readonly IUserService _userService;

    public NotificationController(INotificationService notificationService, IAuditService auditService)
    {
        _notificationService = notificationService;
        _auditService = auditService;
    }

    // GET: /Notification/MyNotifications
    [HttpGet("/Notification/MyNotifications")]
    public async Task<IActionResult> MyNotifications(bool unreadOnly = false, int page = 1, int pageSize = 10, string sortBy = "SentAt", string sortOrder = "desc")
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.GetPagedNotificationsAsync(userId, unreadOnly, page, pageSize);

        if (!result.IsSuccess)
            return View("Error", result.Error);

        return View(result.Value); // View: Views/Notification/MyNotification.cshtml
    }

    // GET: /Notification
    [Authorize(Roles = "Admin,Librarian")]
    [HttpGet]
    [Route("Notification")]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10, string sortBy = "SentAt", string sortOrder = "desc")
    {
        var result = await _notificationService.GetPagedAdminNotificationsAsync(search, page, pageSize, sortBy, sortOrder);

        if (!result.IsSuccess)
            return View("Error", result.Error);

        return View(result.Value);
    }



    // GET: /Notification/Details/5
    public async Task<IActionResult> Details(int id)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
        var result = await _notificationService.GetNotificationDetailAsync(id, userId, isStaff);
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Read,
            EntityType = "Notification",
            EntityId = id.ToString(),
            Details = result.IsSuccess ? "Viewed notification details" : "Failed to view notification details",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.Error
        });
        if (!result.IsSuccess) return View("Error", result.Error);
        return View(result.Value); // View: Views/Notification/Details.cshtml
    }

    // GET: /Notification/Create
    [Authorize(Roles = "Admin,Librarian")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }


    // POST: /Notification/Create (Staff only)
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] NotificationCreateDto dto)
    {
        if (!User.TryGetUserId(out int userId))
            return RedirectToAction("Login", "Auth");
        if (dto.Subject.Length > 200)
            ModelState.AddModelError("Subject", "Subject cannot be longer than 200 characters.");
        if (dto.Message.Length > 500)
            ModelState.AddModelError("Message", "Message cannot be longer than 500 characters.");

        if (!ModelState.IsValid)
            return View(dto);

        var result = await _notificationService.CreateNotificationAsync(dto);

        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Create,
            EntityType = "Notification",
            EntityId = result.IsSuccess ? result.Value.Id.ToString() : null,
            EntityName = dto.Subject,
            Details = result.IsSuccess ? $"Created notification: {dto.Subject}" : $"Failed to create notification: {dto.Subject}",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.Error
        });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(dto);
        }

        return RedirectToAction("Index");
    }


    // POST: /Notification/CreateBulk (Staff only)
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    public async Task<IActionResult> CreateBulk([FromForm] NotificationBatchCreateDto dto)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.CreateNotificationsBulkAsync(dto);
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Create,
            EntityType = "Notification",
            Details = result.IsSuccess ? $"Bulk created notifications for {dto.UserIds.Count} users: {dto.Subject}" : $"Failed bulk create: {dto.Subject}",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.Error
        });
        if (!result.IsSuccess) return BadRequest(result.Error);
        return RedirectToAction("Index");
    }

    // POST: /Notification/UpdateStatus (Staff only)
    [Authorize(Roles = "Admin,Librarian")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromForm] NotificationUpdateStatusDto dto)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.UpdateNotificationStatusAsync(dto);
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Update,
            EntityType = "Notification",
            EntityId = dto.NotificationId.ToString(),
            Details = result.IsSuccess ? $"Updated notification status to {dto.Status}" : $"Failed to update notification status to {dto.Status}",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.Error
        });
        if (!result.IsSuccess) return BadRequest(result.Error);
        return RedirectToAction("Index");
    }

    // POST: /Notification/MarkAsRead
    [HttpPost]
    public async Task<IActionResult> MarkAsRead([FromForm] NotificationMarkAsReadDto dto)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.MarkAsReadAsync(dto, userId);

        int successCount = 0;
        List<(int Id, string Reason)> failures = new();

        if (result.IsSuccess)
        {
            (successCount, failures) = result.Value;
        }

        // Ghi log audit
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Update,
            EntityType = "Notification",
            Details = result.IsSuccess
                ? $"Marked {successCount} notifications as read. Failures: {string.Join(", ", failures.Select(f => $"Id {f.Id}: {f.Reason}"))}"
                : $"Failed to mark notifications as read.",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess
                ? (failures.Count > 0 ? string.Join(", ", failures.Select(f => $"Id {f.Id}: {f.Reason}")) : null)
                : result.Error
        });

        if (!result.IsSuccess)
        {
            if (result.Error == "No unread notifications found to mark as read.")
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            TempData["MarkAsReadFailures"] = result.Error;
            return BadRequest(result.Error);
        }


        if (failures.Count > 0)
        {
            TempData["MarkAsReadFailures"] = string.Join(", ", failures.Select(f => $"Id {f.Id}: {f.Reason}"));
        }

        TempData["MarkAsReadSuccess"] = $"Marked {successCount} notifications as read.";
        return RedirectToAction("MyNotifications");
    }


    // GET: /Notification/UnreadCount
    public async Task<IActionResult> UnreadCount()
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized();
        }

        var result = await _notificationService.GetUnreadCountAsync(userId);
        if (!result.IsSuccess) return Json(new { count = 0 });
        return Json(new { count = result.Value });
    }

    // GET: /Notification/TriggerOverdueJob (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TriggerOverdueJob()
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.SendOverdueNotificationsAsync();
        var (sent, errors) = result.IsSuccess ? result.Value : (0, new List<string>());
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Other,
            EntityType = "NotificationJob",
            Details = result.IsSuccess ? $"Triggered overdue notification job: {sent} sent. Errors: {string.Join("; ", errors)}" : "Failed to trigger overdue notification job",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? (errors.Count > 0 ? string.Join("; ", errors) : null) : result.Error
        });
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { sent, errors });
    }

    // GET: /Notification/TriggerAvailabilityJob (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TriggerAvailabilityJob()
    {
        if (!User.TryGetUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _notificationService.SendAvailabilityNotificationsAsync();
        var (sent, errors) = result.IsSuccess ? result.Value : (0, new List<string>());
        await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
        {
            UserId = userId,
            ActionType = AuditActionType.Other,
            EntityType = "NotificationJob",
            Details = result.IsSuccess ? $"Triggered availability notification job: {sent} sent. Errors: {string.Join("; ", errors)}" : "Failed to trigger availability notification job",
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? (errors.Count > 0 ? string.Join("; ", errors) : null) : result.Error
        });
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { sent, errors });
    }
}
