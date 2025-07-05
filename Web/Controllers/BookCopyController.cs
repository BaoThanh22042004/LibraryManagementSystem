using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;
using Application.Validators;
using Web.Extensions;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for book copy management operations.
    /// Implements UC015 (Add Copy), UC016 (Update Copy Status), and UC017 (Remove Copy).
    /// </summary>
	[Authorize(Roles = "Admin,Librarian")]
    public class BookCopyController : Controller
    {
        private readonly IBookCopyService _bookCopyService;
        private readonly IBookService _bookService;
        private readonly ILogger<BookCopyController> _logger;
        private readonly CreateBookCopyDtoValidator _createBookCopyValidator;
        private readonly CreateMultipleBookCopiesDtoValidator _createMultipleBookCopiesValidator;
        private readonly UpdateBookCopyStatusDtoValidator _updateBookCopyStatusValidator;

		public BookCopyController(
            IBookCopyService bookCopyService,
            IBookService bookService,
            ILogger<BookCopyController> logger,
            CreateBookCopyDtoValidator createBookCopyValidator,
            CreateMultipleBookCopiesDtoValidator createMultipleBookCopiesValidator,
            UpdateBookCopyStatusDtoValidator updateBookCopyStatusValidator)

		{
            _bookCopyService = bookCopyService;
            _bookService = bookService;
            _logger = logger;
			_createBookCopyValidator = createBookCopyValidator;
            _createMultipleBookCopiesValidator = createMultipleBookCopiesValidator;
            _updateBookCopyStatusValidator = updateBookCopyStatusValidator;
		}

        /// <summary>
        /// Displays book copy details.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _bookCopyService.GetBookCopyByIdAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book copy details: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Book");
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book copy details");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving book copy details.";
                return RedirectToAction("Index", "Book");
            }
        }

        /// <summary>
        /// Displays create book copy form.
        /// Implements UC015 (Add Copy).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(int bookId)
        {
            try
            {
                // Verify that the book exists
                var bookResult = await _bookService.GetBookByIdAsync(bookId);
                if (!bookResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book for copy creation: {Error}", bookResult.Error);
                    TempData["ErrorMessage"] = bookResult.Error;
                    return RedirectToAction("Index", "Book");
                }

                ViewBag.Book = bookResult.Value;
                
                // Get a suggested unique copy number
                var copyNumberResult = await _bookCopyService.GenerateUniqueCopyNumberAsync(bookId);
                var copyNumber = copyNumberResult.IsSuccess ? copyNumberResult.Value : string.Empty;

                return View(new CreateBookCopyDto 
                { 
                    BookId = bookId,
                    CopyNumber = copyNumber,
                    Status = CopyStatus.Available
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create book copy form");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create book copy form.";
                return RedirectToAction("Details", "Book", new { id = bookId });
            }
        }

        /// <summary>
        /// Processes create book copy request.
        /// Implements UC015 (Add Copy).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookCopyDto model)
        {
            try
            {
                var validationResult = _createBookCopyValidator.Validate(model);
				if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                    if (bookResult.IsSuccess)
                    {
                        ViewBag.Book = bookResult.Value;
                    }
                    
                    return View(model);
                }

                // Check if copy number already exists for this book
                if (!string.IsNullOrEmpty(model.CopyNumber))
                {
                    var copyNumberExistsResult = await _bookCopyService.CopyNumberExistsAsync(model.BookId, model.CopyNumber);
                    if (copyNumberExistsResult.IsSuccess && copyNumberExistsResult.Value)
                    {
                        ModelState.AddModelError("CopyNumber", "This copy number already exists for the book.");
                        
                        // Retrieve book information again
                        var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                        if (bookResult.IsSuccess)
                        {
                            ViewBag.Book = bookResult.Value;
                        }
                        
                        return View(model);
                    }
                }

                var result = await _bookCopyService.CreateBookCopyAsync(model);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create book copy: {Error}", result.Error);
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Retrieve book information again
                    var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                    if (bookResult.IsSuccess)
                    {
                        ViewBag.Book = bookResult.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Book copy created successfully: {CopyId} - {CopyNumber}", result.Value.Id, result.Value.CopyNumber);
                TempData["SuccessMessage"] = $"Book copy '{result.Value.CopyNumber}' created successfully.";
                
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book copy");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the book copy.");
                
                // Retrieve book information again
                var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                if (bookResult.IsSuccess)
                {
                    ViewBag.Book = bookResult.Value;
                }
                
                return View(model);
            }
        }

        /// <summary>
        /// Displays create multiple book copies form.
        /// Implements UC015 (Add Copy) - bulk creation alternative flow.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateMultiple(int bookId)
        {
            try
            {
                // Verify that the book exists
                var bookResult = await _bookService.GetBookByIdAsync(bookId);
                if (!bookResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book for bulk copy creation: {Error}", bookResult.Error);
                    TempData["ErrorMessage"] = bookResult.Error;
                    return RedirectToAction("Index", "Book");
                }

                ViewBag.Book = bookResult.Value;
                
                return View(new CreateMultipleBookCopiesDto 
                { 
                    BookId = bookId,
                    Quantity = 1,
                    Status = CopyStatus.Available
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create multiple book copies form");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create multiple book copies form.";
                return RedirectToAction("Details", "Book", new { id = bookId });
            }
        }

        /// <summary>
        /// Processes create multiple book copies request.
        /// Implements UC015 (Add Copy) - bulk creation alternative flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(CreateMultipleBookCopiesDto model)
        {
            try
            {
                var validationResult = _createMultipleBookCopiesValidator.Validate(model);
				if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                    if (bookResult.IsSuccess)
                    {
                        ViewBag.Book = bookResult.Value;
                    }
                    
                    return View(model);
                }

                var result = await _bookCopyService.CreateMultipleBookCopiesAsync(model);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create multiple book copies: {Error}", result.Error);
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Retrieve book information again
                    var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                    if (bookResult.IsSuccess)
                    {
                        ViewBag.Book = bookResult.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Multiple book copies created successfully: {BookId}, Quantity: {Quantity}", 
                    model.BookId, model.Quantity);
                TempData["SuccessMessage"] = $"{model.Quantity} book copies created successfully.";
                
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple book copies");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating book copies.");
                
                // Retrieve book information again
                var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
                if (bookResult.IsSuccess)
                {
                    ViewBag.Book = bookResult.Value;
                }
                
                return View(model);
            }
        }

        /// <summary>
        /// Displays update book copy status form.
        /// Implements UC016 (Update Copy Status).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            try
            {
                var result = await _bookCopyService.GetBookCopyByIdAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book copy for status update: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Book");
                }

                var copy = result.Value;
                var updateDto = new UpdateBookCopyStatusDto
                {
                    Id = copy.Id,
                    Status = copy.Status,
                    Notes = string.Empty
                };

                ViewBag.Copy = copy;
                
                // Check for active loans (BR-10)
                var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
                ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book copy for status update");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book copy.";
                return RedirectToAction("Index", "Book");
            }
        }

        /// <summary>
        /// Processes update book copy status request.
        /// Implements UC016 (Update Copy Status).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateBookCopyStatusDto model)
        {
            try
            {
                var validationResult = _updateBookCopyStatusValidator.Validate(model);
				if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

					// Retrieve copy information again
					var copyResult = await _bookCopyService.GetBookCopyByIdAsync(model.Id);
                    if (copyResult.IsSuccess)
                    {
                        ViewBag.Copy = copyResult.Value;
                        
                        // Check for active loans (BR-10)
                        var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(model.Id);
                        ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;
                    }
                    
                    return View(model);
                }

                // Special validation for BR-10
                if (model.Status == CopyStatus.Available)
                {
                    var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(model.Id);
                    if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
                    {
                        ModelState.AddModelError(string.Empty, "Cannot mark copy as Available while it has active loans. Process the return first.");
                        
                        // Retrieve copy information again
                        var copyResult = await _bookCopyService.GetBookCopyByIdAsync(model.Id);
                        if (copyResult.IsSuccess)
                        {
                            ViewBag.Copy = copyResult.Value;
                            ViewBag.HasActiveLoans = true;
                        }
                        
                        return View(model);
                    }
                }

                var result = await _bookCopyService.UpdateBookCopyStatusAsync(model);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to update book copy status: {Error}", result.Error);
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Retrieve copy information again
                    var copyResult = await _bookCopyService.GetBookCopyByIdAsync(model.Id);
                    if (copyResult.IsSuccess)
                    {
                        ViewBag.Copy = copyResult.Value;
                        
                        // Check for active loans (BR-10)
                        var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(model.Id);
                        ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Book copy status updated successfully: {CopyId} to {Status}", model.Id, model.Status);
                TempData["SuccessMessage"] = $"Book copy status updated successfully to {model.Status}.";
                
                return RedirectToAction("Details", "Book", new { id = result.Value.Book.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book copy status");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the book copy status.");
                
                // Retrieve copy information again
                var copyResult = await _bookCopyService.GetBookCopyByIdAsync(model.Id);
                if (copyResult.IsSuccess)
                {
                    ViewBag.Copy = copyResult.Value;
                    
                    // Check for active loans (BR-10)
                    var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(model.Id);
                    ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;
                }
                
                return View(model);
            }
        }

        /// <summary>
        /// Displays delete book copy confirmation page.
        /// Implements UC017 (Remove Copy).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _bookCopyService.GetBookCopyByIdAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book copy for deletion: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Book");
                }

                // Check for active loans
                var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
                if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete copy because it has active loans.";
                    return RedirectToAction("Details", "Book", new { id = result.Value.BookId });
                }

                // Check for active reservations
                var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(id);
                if (hasActiveReservations.IsSuccess && hasActiveReservations.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete copy because it has active reservations.";
                    return RedirectToAction("Details", "Book", new { id = result.Value.BookId });
                }

                ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;
                ViewBag.HasActiveReservations = hasActiveReservations.IsSuccess && hasActiveReservations.Value;

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book copy for deletion");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book copy.";
                return RedirectToAction("Index", "Book");
            }
        }

        /// <summary>
        /// Processes delete book copy request.
        /// Implements UC017 (Remove Copy).
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Get book ID for redirection
                var copyResult = await _bookCopyService.GetBookCopyByIdAsync(id);
                if (!copyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book copy for deletion confirmation: {Error}", copyResult.Error);
                    TempData["ErrorMessage"] = copyResult.Error;
                    return RedirectToAction("Index", "Book");
                }
                
                var bookId = copyResult.Value.BookId;
                
                // Check for active loans again (to prevent deletion if loans were created after showing the delete page)
                var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
                if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete copy because it has active loans.";
                    return RedirectToAction("Details", "Book", new { id = bookId });
                }

                // Check for active reservations again
                var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(id);
                if (hasActiveReservations.IsSuccess && hasActiveReservations.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete copy because it has active reservations.";
                    return RedirectToAction("Details", "Book", new { id = bookId });
                }

                var result = await _bookCopyService.DeleteBookCopyAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete book copy: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Details", "Book", new { id = bookId });
                }

                _logger.LogInformation("Book copy deleted successfully: {CopyId}", id);
                TempData["SuccessMessage"] = "Book copy deleted successfully.";
                
                return RedirectToAction("Details", "Book", new { id = bookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book copy");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the book copy.";
                
                // Try to get book ID for redirection
                try
                {
                    var copyResult = await _bookCopyService.GetBookCopyByIdAsync(id);
                    if (copyResult.IsSuccess)
                    {
                        return RedirectToAction("Details", "Book", new { id = copyResult.Value.BookId });
                    }
                }
                catch
                {
                    // Fallback if we can't get the book ID
                    return RedirectToAction("Index", "Book");
                }
                
                return RedirectToAction("Index", "Book");
            }
        }
    }
}