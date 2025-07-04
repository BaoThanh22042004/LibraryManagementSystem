using Application.Common;
using Application.DTOs;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

/// <summary>
/// Controller for loan management operations (UC018-UC021).
/// </summary>
/// <remarks>
/// Implements the following use cases:
/// - UC018: Check Out - Process book checkout for eligible members
/// - UC019: Return Book - Process book returns with fine calculation
/// - UC020: Renew Loan - Extend loan due dates within policy limits
/// - UC021: View Loan History - Display comprehensive loan records
/// </remarks>
[Authorize(Roles = "Admin,Librarian")]
public class LoanController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<LoanController> _logger;

    public LoanController(IMediator mediator, ILogger<LoanController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Display loan management dashboard (UC021 - View Loan History).
    /// </summary>
    /// <param name="memberId">Optional member ID to filter loans</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Loan management dashboard view</returns>
    [HttpGet]
    public async Task<IActionResult> Index(int? memberId = null, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // UC021.0: Normal Flow - Display loan history interface
            var pagedRequest = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize };
            var query = new GetLoansQuery(pagedRequest, memberId);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error;
                return View(new PagedResult<LoanDto>([], 0, pageNumber, pageSize));
            }

            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan data");
            // UC021.0.E3: Data Retrieval Error
            TempData["Error"] = "Failed to retrieve loan data. Please try again.";
            return View(new PagedResult<LoanDto>([], 0, pageNumber, pageSize));
        }
    }

    /// <summary>
    /// Display checkout form (UC018 - Check Out).
    /// </summary>
    /// <returns>Checkout form view</returns>
    [HttpGet]
    public IActionResult Checkout()
    {
        // UC018.0: Normal Flow - Display checkout interface
        return View(new CreateLoanDto());
    }

    /// <summary>
    /// Process book checkout (UC018 - Check Out).
    /// </summary>
    /// <param name="model">Checkout form data</param>
    /// <returns>Redirect to loan details or checkout form on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CreateLoanDto model)
    {
        if (!ModelState.IsValid)
        {
            // UC018.0.E2: Invalid Information Format
            return View(model);
        }

        try
        {
            // UC018.0: Normal Flow - Process checkout
            var command = new CreateLoanCommand(new CreateLoanDto
            {
                MemberId = model.MemberId,
                BookCopyId = model.BookCopyId,
                CustomDueDate = model.CustomDueDate
            });

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Loan record is created with active status
                // POST-2: Book copy status is updated to borrowed
                // POST-3: Due date is established based on library policy
                TempData["Success"] = $"Book successfully checked out. Loan ID: {result.Value}";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }
            else
            {
                // Handle specific exceptions from UC018
                ModelState.AddModelError("", result.Error);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout");
            // UC018.0.E5: System Error
            ModelState.AddModelError("", "System error during checkout. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// Display loan details (UC021 - View Loan History).
    /// </summary>
    /// <param name="id">Loan ID</param>
    /// <returns>Loan details view</returns>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            // UC021.0: Normal Flow - Display specific loan details
            var query = new GetLoanByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return NotFound();
            }

            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan details");
            // UC021.0.E3: Data Retrieval Error
            TempData["Error"] = "Failed to retrieve loan details.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Process book return (UC019 - Return Book).
    /// </summary>
    /// <param name="id">Loan ID to return</param>
    /// <returns>Redirect to loan details or index</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        try
        {
            // UC019.0: Normal Flow - Process book return
            var command = new ReturnBookCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Loan record is updated with return date and returned status
                // POST-2: Book copy status is updated to available
                // POST-4: Overdue fines are calculated and applied if applicable
                TempData["Success"] = "Book successfully returned.";
            }
            else
            {
                // Handle specific exceptions from UC019
                TempData["Error"] = result.Error;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during return");
            // UC019.0.E3: System Error During Processing
            TempData["Error"] = "System error during return processing. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Display loan extension form (UC020 - Renew Loan).
    /// </summary>
    /// <param name="id">Loan ID to extend</param>
    /// <returns>Extension form view</returns>
    [HttpGet]
    public async Task<IActionResult> Extend(int id)
    {
        try
        {
            // UC020.0: Normal Flow - Display extension interface
            var query = new GetLoanByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return NotFound();
            }

            var model = new ExtendLoanDto
            {
                LoanId = result.Value.Id
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loan extension form");
            TempData["Error"] = "Failed to load loan extension form.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Process loan extension (UC020 - Renew Loan).
    /// </summary>
    /// <param name="model">Extension form data</param>
    /// <returns>Redirect to loan details or extension form on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extend(ExtendLoanDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // UC020.0: Normal Flow - Process loan extension
            var command = new ExtendLoanCommand(new ExtendLoanDto
            {
                LoanId = model.LoanId,
                NewDueDate = model.NewDueDate,
                Reason = model.Reason
            });

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // POST-1: Loan due date is updated to the new extended date
                // POST-2: Extension transaction is recorded for audit purposes
                TempData["Success"] = $"Loan successfully extended. New due date: {model.NewDueDate:yyyy-MM-dd}";
                return RedirectToAction(nameof(Details), new { id = model.LoanId });
            }
            else
            {
                // Handle specific exceptions from UC020
                ModelState.AddModelError("", result.Error);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during loan extension");
            ModelState.AddModelError("", "System error during loan extension. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// Display member loan history (UC021 - View Loan History).
    /// </summary>
    /// <param name="memberId">Member ID</param>
    /// <returns>Member loan history view</returns>
    [HttpGet]
    public async Task<IActionResult> MemberHistory(int memberId)
    {
        try
        {
            // UC021.0: Normal Flow - Display member-specific loan history
            var query = new GetLoanHistoryQuery(memberId, 10);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving member loan history");
            // UC021.0.E3: Data Retrieval Error
            TempData["Error"] = "Failed to retrieve member loan history.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display overdue loans report (UC021.2: Overdue Loans Report).
    /// </summary>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Overdue loans report view</returns>
    [HttpGet]
    public async Task<IActionResult> Overdue(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // UC021.2: Overdue Loans Report
            var pagedRequest = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize };
            var query = new GetOverdueLoansQuery(pagedRequest);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error;
                return View(new PagedResult<LoanDto>([], 0, pageNumber, pageSize));
            }

            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue loans");
            TempData["Error"] = "Failed to retrieve overdue loans.";
            return View(new PagedResult<LoanDto>([], 0, pageNumber, pageSize));
        }
    }

    /// <summary>
    /// Check loan eligibility for a member (UC018 - Check Out validation).
    /// </summary>
    /// <param name="memberId">Member ID to check</param>
    /// <returns>JSON result with eligibility status</returns>
    [HttpGet]
    public async Task<IActionResult> CheckEligibility(int memberId)
    {
        try
        {
            // UC018 validation: Check member eligibility
            var query = new CheckLoanEligibilityQuery(memberId);
            var result = await _mediator.Send(query);

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking loan eligibility");
            return Json(new { IsEligible = false, Reasons = new[] { "System error checking eligibility" } });
        }
    }
} 