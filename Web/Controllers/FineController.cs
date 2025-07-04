using Application.DTOs;
using Application.Features.Fines.Commands;
using Application.Features.Fines.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class FineController : Controller
{
    private readonly IMediator _mediator;
    public FineController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: /Fine
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var pagedRequest = new Application.Common.PagedRequest{PageNumber = page, PageSize = pageSize};
        var result = await _mediator.Send(new GetPaginatedFinesQuery(pagedRequest, search));
        return View(result);
    }

    // GET: /Fine/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var fine = await _mediator.Send(new GetFineByIdQuery(id));
        if (fine == null) return NotFound();
        return View(fine);
    }

    // GET: /Fine/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Fine/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFineDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _mediator.Send(new CreateFineCommand(dto));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create fine.");
            return View(dto);
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Fine/Pay/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id)
    {
        var result = await _mediator.Send(new PayFineCommand(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to pay fine.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Fine/Waive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Waive(int id)
    {
        var result = await _mediator.Send(new WaiveFineCommand(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to waive fine.";
        }
        return RedirectToAction(nameof(Index));
    }
} 