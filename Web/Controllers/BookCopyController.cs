using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain.Enums;
using Application.DTOs;
using Application.Common;
using MediatR;
using Application.Features.BookCopies.Commands;
using Application.Features.BookCopies.Queries;
using Application.Features.Books.Queries;

namespace Web.Controllers;

/// <summary>
/// Controller for managing book copies in the library system.
/// Implements UC015 (Add Copy), UC016 (Update Copy Status), and UC017 (Remove Copy).
/// </summary>
/// <remarks>
/// This controller provides the web interface for book copy management operations.
/// All operations require Librarian or Admin role authorization (BR-06).
/// 
/// Use Cases Supported:
/// - UC015: Add Copy - Create new physical copies of existing books
/// - UC016: Update Copy Status - Change copy status with proper validation
/// - UC017: Remove Copy - Delete copies with dependency validation
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can manage copies)
/// - BR-08: Copy Deletion Restriction (Copies with active loans/reservations cannot be deleted)
/// - BR-09: Copy Status Rules (Proper status transitions enforced)
/// - BR-10: Copy Return Validation (Cannot mark as Available without proper return)
/// - BR-22: Audit Logging Requirement (All operations logged)
/// - BR-24: Role-Based Access Control (Proper authorization on all endpoints)
/// </remarks>
[Authorize(Roles = "Librarian,Admin")]
public class BookCopyController : Controller
{
    private readonly ILogger<BookCopyController> _logger;
    private readonly IMediator _mediator;

    public BookCopyController(ILogger<BookCopyController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// GET: BookCopy/Create
    /// Displays the form for creating a new book copy (UC015).
    /// </summary>
    /// <param name="bookId">Optional book ID to pre-select the book</param>
    /// <returns>Create view for book copy</returns>
    public async Task<IActionResult> Create(int? bookId = null)
    {
        try
        {
            var createDto = new CreateBookCopyDto();
            
            if (bookId.HasValue)
            {
                var book = await _mediator.Send(new GetBookByIdQuery(bookId.Value));
                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction("Index", "Book");
                }
                
                createDto.BookId = bookId.Value;
                ViewBag.BookTitle = book.Title;
            }

            return View(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading create book copy page");
            TempData["Error"] = "An error occurred while loading the page.";
            return RedirectToAction("Index", "Book");
        }
    }

    /// <summary>
    /// POST: BookCopy/Create
    /// Creates a new book copy (UC015).
    /// </summary>
    /// <param name="bookCopyDto">The book copy data</param>
    /// <returns>Redirect to book details or create view on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookCopyDto bookCopyDto)
    {
        if (!ModelState.IsValid)
        {
            if (bookCopyDto.BookId > 0)
            {
                try
                {
                    var book = await _mediator.Send(new GetBookByIdQuery(bookCopyDto.BookId));
                    ViewBag.BookTitle = book?.Title;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading book title for display");
                }
            }
            return View(bookCopyDto);
        }

        try
        {
            var book = await _mediator.Send(new GetBookByIdQuery(bookCopyDto.BookId));
            if (book == null)
            {
                ModelState.AddModelError("BookId", "Selected book does not exist.");
                return View(bookCopyDto);
            }

            if (!string.IsNullOrEmpty(bookCopyDto.CopyNumber))
            {
                var copies = await _mediator.Send(new GetBookCopiesByBookIdQuery(bookCopyDto.BookId));
                if (copies.Any(c => c.CopyNumber == bookCopyDto.CopyNumber))
                {
                    ModelState.AddModelError("CopyNumber", "A copy with this number already exists for this book.");
                    ViewBag.BookTitle = book.Title;
                    return View(bookCopyDto);
                }
            }

            var result = await _mediator.Send(new CreateBookCopyCommand(bookCopyDto));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to create book copy.");
                ViewBag.BookTitle = book.Title;
                return View(bookCopyDto);
            }
            TempData["Success"] = "Book copy created successfully.";
            
            return RedirectToAction("Details", "Book", new { id = bookCopyDto.BookId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating book copy for book ID: {BookId}", bookCopyDto.BookId);
            ModelState.AddModelError("", ex.Message);
            
            try
            {
                var book = await _mediator.Send(new GetBookByIdQuery(bookCopyDto.BookId));
                ViewBag.BookTitle = book?.Title;
            }
            catch (Exception bookEx)
            {
                _logger.LogError(bookEx, "Error loading book title for display");
            }
            
            return View(bookCopyDto);
        }
    }

    /// <summary>
    /// GET: BookCopy/Edit/5
    /// Displays the form for editing a book copy (UC016).
    /// </summary>
    /// <param name="id">The book copy ID</param>
    /// <returns>Edit view for book copy</returns>
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
            if (bookCopy == null)
            {
                return NotFound();
            }

            var updateDto = new UpdateBookCopyDto
            {
                CopyNumber = bookCopy.CopyNumber,
                Status = bookCopy.Status
            };

            ViewBag.BookTitle = bookCopy.BookTitle;
            ViewBag.BookId = bookCopy.BookId;

            return View(updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving book copy for edit, ID: {CopyId}", id);
            TempData["Error"] = "An error occurred while retrieving the book copy.";
            return RedirectToAction("Index", "Book");
        }
    }

    /// <summary>
    /// POST: BookCopy/Edit/5
    /// Updates a book copy (UC016).
    /// </summary>
    /// <param name="id">The book copy ID</param>
    /// <param name="bookCopyDto">The updated book copy data</param>
    /// <returns>Redirect to book details or edit view on error</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateBookCopyDto bookCopyDto)
    {
        if (!ModelState.IsValid)
        {
            try
            {
                var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
                ViewBag.BookTitle = bookCopy?.BookTitle;
                ViewBag.BookId = bookCopy?.BookId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading book copy for display");
            }
            return View(bookCopyDto);
        }

        try
        {
            var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
            if (bookCopy == null)
            {
                return NotFound();
            }

            var result = await _mediator.Send(new UpdateBookCopyStatusCommand(id, bookCopyDto.Status));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to update book copy.");
                ViewBag.BookTitle = bookCopy.BookTitle;
                ViewBag.BookId = bookCopy.BookId;
                return View(bookCopyDto);
            }

            TempData["Success"] = "Book copy updated successfully.";
            
            return RedirectToAction("Details", "Book", new { id = bookCopy.BookId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating book copy ID: {CopyId}", id);
            ModelState.AddModelError("", ex.Message);
            
            try
            {
                var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
                ViewBag.BookTitle = bookCopy?.BookTitle;
                ViewBag.BookId = bookCopy?.BookId;
            }
            catch (Exception bookEx)
            {
                _logger.LogError(bookEx, "Error loading book copy for display");
            }
            
            return View(bookCopyDto);
        }
    }

    /// <summary>
    /// GET: BookCopy/Delete/5
    /// Displays the confirmation page for deleting a book copy (UC017).
    /// </summary>
    /// <param name="id">The book copy ID</param>
    /// <returns>Delete confirmation view</returns>
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
            if (bookCopy == null)
            {
                return NotFound();
            }

            ViewBag.BookTitle = bookCopy.BookTitle;
            ViewBag.BookId = bookCopy.BookId;
            return View(bookCopy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading delete book copy page, ID: {CopyId}", id);
            TempData["Error"] = "An error occurred while loading the page.";
            return RedirectToAction("Index", "Book");
        }
    }

    /// <summary>
    /// POST: BookCopy/Delete/5
    /// Deletes a book copy (UC017).
    /// </summary>
    /// <param name="id">The book copy ID</param>
    /// <returns>Redirect to book details</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var bookCopy = await _mediator.Send(new GetBookCopyByIdQuery(id));
            if (bookCopy == null)
            {
                return NotFound();
            }

            var result = await _mediator.Send(new DeleteBookCopyCommand(id));
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error ?? "Failed to delete book copy.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Book copy deleted successfully.";
            return RedirectToAction("Details", "Book", new { id = bookCopy.BookId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting book copy ID: {CopyId}", id);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    /// <summary>
    /// POST: BookCopy/UpdateStatus/5
    /// Updates the status of a book copy (UC016).
    /// </summary>
    /// <param name="id">The book copy ID</param>
    /// <param name="status">The new status</param>
    /// <returns>JSON result</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] CopyStatus status)
    {
        try
        {
            var result = await _mediator.Send(new UpdateBookCopyStatusCommand(id, status));
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating status for book copy ID: {CopyId}", id);
            return StatusCode(500, ex.Message);
        }
    }
} 