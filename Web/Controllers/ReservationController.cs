using Application.Common;
using Application.DTOs;
using Application.Features.Reservations.Commands;
using Application.Features.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

/// <summary>
/// Controller for reservation management operations (UC022-UC025).
/// </summary>
/// <remarks>
/// Implements the following use cases:
/// - UC022: Reserve Book - Create reservations for unavailable books
/// - UC023: Cancel Reservation - Cancel active reservations
/// - UC024: Fulfill Reservation - Process reservation fulfillment when books become available
/// - UC025: View Reservations - Display comprehensive reservation records
/// </remarks>
[Authorize(Roles = "Admin,Librarian")]
public class ReservationController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(IMediator mediator, ILogger<ReservationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Display reservation management dashboard (UC025 - View Reservations).
    /// </summary>
    /// <param name="searchTerm">Optional search term for filtering</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Reservation management dashboard view</returns>
    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // UC025.0: Normal Flow - Display reservation management interface
            var pagedRequest = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize };
            var query = new GetPaginatedReservationsQuery(pagedRequest, searchTerm);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                // UC025.0.E1: No Reservations Found
                TempData["Info"] = "No reservations found matching your criteria.";
                return View(new PagedResult<ReservationDto>([], 0, pageNumber, pageSize));
            }

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation data");
            // UC025.0.E3: Data Retrieval Error
            TempData["Error"] = "Failed to retrieve reservation data. Please try again.";
            return View(new PagedResult<ReservationDto>([], 0, pageNumber, pageSize));
        }
    }

    /// <summary>
    /// Display reservation creation form (UC022 - Reserve Book).
    /// </summary>
    /// <returns>Reservation creation form view</returns>
    [HttpGet]
    public IActionResult Create()
    {
        // UC022.0: Normal Flow - Display reservation form
        return View(new CreateReservationDto());
    }

    /// <summary>
    /// Process book reservation (UC022 - Reserve Book).
    /// </summary>
    /// <param name="model">Reservation form data</param>
    /// <returns>Redirect to reservation details or creation form on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationDto model)
    {
        if (!ModelState.IsValid)
        {
            // UC022.0.E2: Invalid Information Format
            return View(model);
        }

        try
        {
            // UC022.0: Normal Flow - Process reservation creation
            var command = new CreateReservationCommand(new CreateReservationDto
            {
                MemberId = model.MemberId,
                BookId = model.BookId
            });

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Reservation record is created with pending status
                // POST-2: Member is placed in the reservation queue for the book
                // POST-3: System records reservation date and member's queue position
                TempData["Success"] = $"Reservation successfully created. Reservation ID: {result.Value}";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            else
            {
                // Handle specific exceptions from UC022
                ModelState.AddModelError("", result.Error);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reservation creation");
            ModelState.AddModelError("", "System error during reservation creation. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// Display reservation details (UC025 - View Reservations).
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <returns>Reservation details view</returns>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            // UC025.0: Normal Flow - Display specific reservation details
            var query = new GetReservationByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation details");
            // UC025.0.E3: Data Retrieval Error
            TempData["Error"] = "Failed to retrieve reservation details.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Process reservation cancellation (UC023 - Cancel Reservation).
    /// </summary>
    /// <param name="id">Reservation ID to cancel</param>
    /// <returns>Redirect to reservation index or details</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            // UC023.0: Normal Flow - Process reservation cancellation
            var command = new CancelReservationCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Reservation status is updated to cancelled
                // POST-2: Member is removed from the reservation queue
                // POST-3: Other members' queue positions are automatically adjusted
                TempData["Success"] = "Reservation successfully cancelled.";
            }
            else
            {
                // Handle specific exceptions from UC023
                TempData["Error"] = result.Error;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reservation cancellation");
            TempData["Error"] = "System error during reservation cancellation. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Display reservation fulfillment form (UC024 - Fulfill Reservation).
    /// </summary>
    /// <param name="id">Reservation ID to fulfill</param>
    /// <returns>Fulfillment form view</returns>
    [HttpGet]
    public async Task<IActionResult> Fulfill(int id)
    {
        try
        {
            // UC024.0: Normal Flow - Display fulfillment interface
            var query = new GetReservationByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reservation fulfillment form");
            TempData["Error"] = "Failed to load reservation fulfillment form.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Process reservation fulfillment (UC024 - Fulfill Reservation).
    /// </summary>
    /// <param name="model">Fulfillment form data</param>
    /// <returns>Redirect to reservation details or fulfillment form on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fulfill(ReservationDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // UC024.0: Normal Flow - Process reservation fulfillment
            var command = new FulfillReservationCommand(model.Id, model.BookCopyId ?? 0);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Reservation status is updated to fulfilled
                // POST-2: Book copy is held specifically for the member
                // POST-3: Member is notified of book availability
                // POST-4: Pickup deadline is established (typically 72 hours)
                TempData["Success"] = "Reservation successfully fulfilled. Member has been notified.";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            else
            {
                // Handle specific exceptions from UC024
                ModelState.AddModelError("", result.Error);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reservation fulfillment");
            // UC024.0.E4: Notification Failure
            ModelState.AddModelError("", "System error during reservation fulfillment. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// Display active reservations for a book (UC025.2: Book-Specific Reservations).
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <returns>Book-specific reservations view</returns>
    [HttpGet]
    public async Task<IActionResult> ByBook(int bookId)
    {
        try
        {
            // UC025.2: Book-Specific Reservations
            var query = new GetReservationsByBookIdQuery(bookId);
            var result = await _mediator.Send(query);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book reservations");
            TempData["Error"] = "Failed to retrieve book reservations.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display active reservations for a member (UC025 - View Reservations).
    /// </summary>
    /// <param name="memberId">Member ID</param>
    /// <returns>Member-specific reservations view</returns>
    [HttpGet]
    public async Task<IActionResult> ByMember(int memberId)
    {
        try
        {
            // UC025.0: Normal Flow - Display member-specific reservations
            var query = new GetReservationsByMemberIdQuery(memberId);
            var result = await _mediator.Send(query);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving member reservations");
            TempData["Error"] = "Failed to retrieve member reservations.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display active reservations only (UC025.1: Active Reservations Only).
    /// </summary>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Active reservations view</returns>
    [HttpGet]
    public async Task<IActionResult> Active(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // UC025.1: Active Reservations Only
            var query = new GetActiveReservationsQuery();
            var result = await _mediator.Send(query);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active reservations");
            TempData["Error"] = "Failed to retrieve active reservations.";
            return View(new PagedResult<ReservationDto>([], 0, pageNumber, pageSize));
        }
    }

    /// <summary>
    /// Process bulk reservation expiration (UC023 - Cancel Reservation).
    /// </summary>
    /// <returns>Redirect to reservation index</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpireAll()
    {
        try
        {
            // UC023: Bulk cancellation of expired reservations
            var command = new ExpireReservationsCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                TempData["Success"] = "Expired reservations have been processed.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reservation expiration");
            TempData["Error"] = "System error during reservation expiration. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get next active reservation for a book (UC024 - Fulfill Reservation).
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <returns>JSON result with next reservation details</returns>
    [HttpGet]
    public async Task<IActionResult> GetNextReservation(int bookId)
    {
        try
        {
            // UC024: Get next member in queue for fulfillment
            var query = new GetNextActiveReservationQuery(bookId);
            var result = await _mediator.Send(query);

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next reservation");
            return Json(new { Success = false, Message = "Failed to get next reservation" });
        }
    }
} 