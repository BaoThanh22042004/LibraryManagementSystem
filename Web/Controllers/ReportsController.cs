using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Application.DTOs;
using Domain.Enums;
using Web.Extensions;

namespace Web.Controllers;

[Authorize(Roles = "Admin,Librarian")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, IAuditService auditService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _auditService = auditService;
        _logger = logger;
    }

    // GET: /Reports
    public IActionResult Index()
    {
        // Show available report types and formats
        return View();
    }

    // POST: /Reports/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(ReportRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid report parameters.";
            return View("Index", request);
        }
        try
        {
            var result = await _reportService.GenerateReportAsync(request);
            if (!result.IsSuccess)
            {
                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = AuditActionType.Export,
                        EntityType = "Report",
                        Details = $"Failed to generate report: {request.ReportType} ({request.Format}) - {result.Error}",
                        IsSuccess = false
                    });
                }
                TempData["ErrorMessage"] = result.Error;
                return View("Index", request);
            }
            // Audit log for success
            if (User.TryGetUserId(out int staffIdSuccess))
            {
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = staffIdSuccess,
                    ActionType = AuditActionType.Export,
                    EntityType = "Report",
                    Details = $"Generated report: {request.ReportType} ({request.Format})",
                    IsSuccess = true
                });
            }
            // Return file for download
            TempData["SuccessMessage"] = $"Report '{request.ReportType}' generated successfully.";
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            if (User.TryGetUserId(out int staffIdFail))
            {
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = staffIdFail,
                    ActionType = AuditActionType.Export,
                    EntityType = "Report",
                    Details = $"Exception during report generation: {request.ReportType} ({request.Format}) - {ex.Message}",
                    IsSuccess = false
                });
            }
            TempData["ErrorMessage"] = "Unable to generate report.";
            return View("Index", request);
        }
    }
} 