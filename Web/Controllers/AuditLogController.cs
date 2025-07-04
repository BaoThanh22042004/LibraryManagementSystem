using Application.DTOs;
using Application.Features.AuditLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class AuditLogController : Controller
{
    private readonly IMediator _mediator;
    public AuditLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: /AuditLog
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterDto? filter, int page = 1, int pageSize = 20)
    {
        var pagedRequest = new Application.Common.PagedRequest{PageNumber = page, PageSize = pageSize};
        var result = await _mediator.Send(new GetPaginatedAuditLogsQuery(pagedRequest, filter));
        return View(result);
    }

    // GET: /AuditLog/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var log = await _mediator.Send(new GetAuditLogByIdQuery(id));
        if (log == null) return NotFound();
        return View(log);
    }
} 