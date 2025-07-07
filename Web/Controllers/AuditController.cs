using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers;

[Authorize(Roles = "Admin,Librarian")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditLogSearchRequest? search = null)
    {
        search ??= new AuditLogSearchRequest { Page = 1, PageSize = 20 };
        var result = await _auditService.SearchAuditLogsAsync(search);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error;
            return View(new Application.Common.PagedResult<AuditLogResponse>());
        }
        // Audit log for viewing logs
        if (User.TryGetUserId(out int staffId))
        {
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                UserId = staffId,
                ActionType = Domain.Enums.AuditActionType.Read,
                EntityType = "AuditLog",
                Details = $"Viewed audit logs (page {search.Page}, filters: user={search.UserId}, action={search.ActionType}, entity={search.EntityType})",
                IsSuccess = true
            });
        }
        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(AuditLogSearchRequest? search = null)
    {
        search ??= new AuditLogSearchRequest { Page = 1, PageSize = 1000 };
        var result = await _auditService.SearchAuditLogsAsync(search);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Index));
        }
        var csv = CsvExportExtensions.ToCsv(result.Value.Items);
        var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMdd}.csv";
        // Audit log for export
        if (User.TryGetUserId(out int staffId))
        {
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                UserId = staffId,
                ActionType = Domain.Enums.AuditActionType.Export,
                EntityType = "AuditLog",
                Details = $"Exported audit logs (filters: user={search.UserId}, action={search.ActionType}, entity={search.EntityType})",
                IsSuccess = true
            });
        }
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
} 