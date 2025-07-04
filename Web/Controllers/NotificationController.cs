using Application.DTOs;
using Application.Features.Notifications.Commands;
using Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class NotificationController : Controller
{
    private readonly IMediator _mediator;
    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: /Notification
    public async Task<IActionResult> Index(int? userId, string? filter, int page = 1, int pageSize = 10)
    {
        var pagedRequest = new Application.Common.PagedRequest { PageNumber = page, PageSize = pageSize };
        var result = await _mediator.Send(new GetPaginatedNotificationsQuery(pagedRequest, filter));
        return View(result);
    }

    // GET: /Notification/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var notification = await _mediator.Send(new GetNotificationByIdQuery(id));
        if (notification == null) return NotFound();
        return View(notification);
    }

    // GET: /Notification/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Notification/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateNotificationDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _mediator.Send(new CreateNotificationCommand(dto));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create notification.");
            return View(dto);
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Notification/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, UpdateNotificationDto dto)
    {
        var result = await _mediator.Send(new UpdateNotificationCommand(id, dto));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to update notification status.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Notification/MarkAsRead/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to mark as read.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Notification/MarkAllAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(int? userId)
    {
        if (userId.HasValue)
        {
            var result = await _mediator.Send(new MarkAllAsReadCommand(userId.Value));
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error ?? "Failed to mark all as read.";
            }
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Notification/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to delete notification.";
        }
        return RedirectToAction(nameof(Index));
    }
}