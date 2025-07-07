using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;
using Application.Validators;
using Web.Extensions;
using Application.Common;
using FluentValidation;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for book management operations.
    /// Implements UC010 (Add Book), UC011 (Update Book), UC012 (Delete Book), 
    /// UC013 (Search Books), and UC014 (Browse by Category).
    /// Business Rules: BR-06, BR-07, BR-22, BR-24
    /// </summary>
    public class BookController : Controller
    {
        private readonly IBookService _bookService;
        private readonly ICategoryService _categoryService;
        private readonly IAuditService _auditService;
        private readonly ILogger<BookController> _logger;
        private readonly IValidator<BookSearchRequest> _searchParametersValidator;
        private readonly IValidator<CreateBookRequest> _createBookValidator;
        private readonly IValidator<UpdateBookRequest> _updateBookValidator;

        public BookController(
            IBookService bookService,
            ICategoryService categoryService,
            IAuditService auditService,
            ILogger<BookController> logger,
            IValidator<BookSearchRequest> searchParametersValidator,
            IValidator<CreateBookRequest> createBookValidator,
            IValidator<UpdateBookRequest> updateBookValidator)
        {
            _bookService = bookService;
            _categoryService = categoryService;
            _auditService = auditService;
            _logger = logger;
            _searchParametersValidator = searchParametersValidator;
            _createBookValidator = createBookValidator;
            _updateBookValidator = updateBookValidator;
        }

        /// <summary>
        /// Displays book listing page with search and filter functionality.
        /// Implements UC013 (Search Books) and UC014 (Browse by Category).
        /// BR-24: Accessible to all users (guests and authenticated)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(BookSearchRequest searchParams)
        {
            try
            {
                var validationResult = _searchParametersValidator.Validate(searchParams);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

                    return View(new PagedResult<BookBasicDto>
                    {
                        Page = searchParams.Page,
                        PageSize = searchParams.PageSize,
                    });
                }

                // Set default values if not provided
                searchParams.Page = searchParams.Page <= 0 ? 1 : searchParams.Page;
                searchParams.PageSize = searchParams.PageSize <= 0 ? 10 : searchParams.PageSize;

                // Get books based on search parameters
                var booksResult = await _bookService.SearchBooksAsync(searchParams);

                if (!booksResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve books: {Error}", booksResult.Error);
                    TempData["ErrorMessage"] = "Failed to retrieve books. Please try again later.";
                    return View(new PagedResult<BookBasicDto>
                    {
                        Page = searchParams.Page,
                        PageSize = searchParams.PageSize,
                    });
                }

                // Get all categories for filtering
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();

                if (categoriesResult.IsSuccess)
                {
                    ViewBag.Categories = categoriesResult.Value;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryDto>();
                }

                // Pass search parameters back to view for maintaining filter state
                ViewBag.SearchTerm = searchParams.SearchTerm;
                ViewBag.CategoryId = searchParams.CategoryId;

                return View(booksResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving books.";
                return View();
            }
        }

        /// <summary>
        /// Displays book details page.
        /// Part of UC013 (Search Books) - detailed view.
        /// BR-24: Accessible to all users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _bookService.GetBookByIdAsync(id);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book details for {BookId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving book details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays create book form.
        /// Implements UC010 (Add Book).
        /// BR-06: Only Librarian or Admin can add, edit, or delete books
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get all categories for selection
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();

                if (categoriesResult.IsSuccess)
                {
                    ViewBag.Categories = categoriesResult.Value;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryDto>();
                    TempData["WarningMessage"] = "Could not load categories. You can add categories to the book later.";
                }

                return View(new CreateBookRequest { InitialCopies = 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create book form");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create book form.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes create book request.
        /// Implements UC010 (Add Book).
        /// BR-06, BR-22: Book management with audit logging
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(CreateBookRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var validationResult = _createBookValidator.Validate(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                // Check if ISBN already exists
                var isbnExistsResult = await _bookService.IsbnExistsAsync(model.ISBN);
                if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
                {
                    ModelState.AddModelError("ISBN", "A book with this ISBN already exists.");

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                var result = await _bookService.CreateBookAsync(model);

                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                // Audit successful book creation
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Create,
                    EntityType = "Book",
                    EntityId = result.Value.Id.ToString(),
                    EntityName = result.Value.Title,
                    Details = $"Book created successfully. ISBN: {model.ISBN}, Author: {model.Author}, InitialCopies: {model.InitialCopies}, Categories: {model.CategoryIds?.Count ?? 0}",
                    IsSuccess = true
                });

                _logger.LogInformation("Book created successfully: {BookId} - {BookTitle} by user {UserId} at {Time}",
                    result.Value.Id, result.Value.Title, userId, DateTime.UtcNow);
                TempData["SuccessMessage"] = $"Book '{result.Value.Title}' created successfully with {model.InitialCopies} copies.";

                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book by user {UserId}", User.GetUserId());
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the book.");

                // Get categories for the form again
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                if (categoriesResult.IsSuccess)
                {
                    ViewBag.Categories = categoriesResult.Value;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryDto>();
                }

                return View(model);
            }
        }

        /// <summary>
        /// Displays edit book form.
        /// Implements UC011 (Update Book).
        /// BR-06: Only Librarian or Admin can manage books
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var result = await _bookService.GetBookByIdAsync(id);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                var book = result.Value;
                var updateDto = new UpdateBookRequest
                {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    ISBN = book.ISBN,
                    Publisher = book.Publisher,
                    Description = book.Description,
                    CoverImageUrl = book.CoverImageUrl,
                    PublicationDate = book.PublicationDate,
                    Status = book.Status,
                    CategoryIds = [.. book.Categories.Select(c => c.Id)]
				};

                // Get all categories for selection
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                if (categoriesResult.IsSuccess)
                {
                    ViewBag.Categories = categoriesResult.Value;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryDto>();
                    TempData["WarningMessage"] = "Could not load categories. Existing categories will be preserved.";
                }

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book for editing {BookId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes edit book request.
        /// Implements UC011 (Update Book).
        /// BR-06, BR-22: Book management with audit logging
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(UpdateBookRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var validationResult = _updateBookValidator.Validate(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                // Check if updated ISBN already exists (excluding current book)
                var isbnExistsResult = await _bookService.IsbnExistsAsync(model.ISBN, model.Id);
                if (isbnExistsResult.IsSuccess && isbnExistsResult.Value)
                {
                    ModelState.AddModelError("ISBN", "A book with this ISBN already exists.");

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                var result = await _bookService.UpdateBookAsync(model);

                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);

                    // Get categories for the form again
                    var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                    if (categoriesResult.IsSuccess)
                    {
                        ViewBag.Categories = categoriesResult.Value;
                    }
                    else
                    {
                        ViewBag.Categories = new List<CategoryDto>();
                    }

                    return View(model);
                }

                // Audit successful book update
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Update,
                    EntityType = "Book",
                    EntityId = result.Value.Id.ToString(),
                    EntityName = result.Value.Title,
                    Details = $"Book updated successfully. ISBN: {model.ISBN}, Status: {model.Status}, Categories: {model.CategoryIds?.Count ?? 0}, Updated by: {userId}",
                    IsSuccess = true
                });

                _logger.LogInformation("Book updated successfully: {BookId} - {BookTitle} by user {UserId} at {Time}",
                    result.Value.Id, result.Value.Title, userId, DateTime.UtcNow);
                TempData["SuccessMessage"] = $"Book '{result.Value.Title}' updated successfully.";

                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book {BookId} by user {UserId}", model.Id, User.GetUserId());
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the book.");

                // Get categories for the form again
                var categoriesResult = await _categoryService.GetAllCategoriesAsync();
                if (categoriesResult.IsSuccess)
                {
                    ViewBag.Categories = categoriesResult.Value;
                }
                else
                {
                    ViewBag.Categories = new List<CategoryDto>();
                }

                return View(model);
            }
        }

        /// <summary>
        /// Displays delete book confirmation page.
        /// Implements UC012 (Delete Book).
        /// BR-06, BR-07: Book management and deletion restrictions
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _bookService.GetBookByIdAsync(id);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // BR-07: Check for active loans
                var hasLoansResult = await _bookService.BookHasActiveLoansAsync(id);
                if (hasLoansResult.IsSuccess && hasLoansResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active loans.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // BR-07: Check for active reservations
                var hasReservationsResult = await _bookService.BookHasActiveReservationsAsync(id);
                if (hasReservationsResult.IsSuccess && hasReservationsResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active reservations.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ViewBag.HasActiveLoans = hasLoansResult.IsSuccess && hasLoansResult.Value;
                ViewBag.HasActiveReservations = hasReservationsResult.IsSuccess && hasReservationsResult.Value;

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book for deletion {BookId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes delete book request.
        /// Implements UC012 (Delete Book).
        /// BR-06, BR-07, BR-22: Book management with business rules and audit logging
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Get book details for audit logging
                var bookDetailsResult = await _bookService.GetBookByIdAsync(id);
                var bookTitle = bookDetailsResult.IsSuccess ? bookDetailsResult.Value.Title : "Unknown";
                var bookISBN = bookDetailsResult.IsSuccess ? bookDetailsResult.Value.ISBN : "Unknown";

                // BR-07: Check for active loans again (to prevent deletion if loans were created after showing the delete page)
                var hasLoansResult = await _bookService.BookHasActiveLoansAsync(id);
                if (hasLoansResult.IsSuccess && hasLoansResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active loans.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // BR-07: Check for active reservations again
                var hasReservationsResult = await _bookService.BookHasActiveReservationsAsync(id);
                if (hasReservationsResult.IsSuccess && hasReservationsResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active reservations.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _bookService.DeleteBookAsync(id);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Audit successful book deletion
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = userId,
                    ActionType = AuditActionType.Delete,
                    EntityType = "Book",
                    EntityId = id.ToString(),
                    EntityName = bookTitle,
                    Details = $"Book deleted successfully. ISBN: {bookISBN}, Deleted by: {userId}",
                    IsSuccess = true
                });

                _logger.LogInformation("Book deleted successfully: {BookId} - {BookTitle} by user {UserId} at {Time}",
                    id, bookTitle, userId, DateTime.UtcNow);
                TempData["SuccessMessage"] = "Book deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {BookId} by user {UserId}", id, User.GetUserId());
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the book.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// API endpoint to check if an ISBN already exists.
        /// Used for client-side validation during create/edit operations.
        /// </summary>
        public async Task<IActionResult> IsbnExists(string isbn, int? id = null)
        {
            try
            {
                var result = await _bookService.IsbnExistsAsync(isbn, id);
                return Ok(new { exists = result.IsSuccess && result.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if ISBN exists: {ISBN}", isbn);
                return StatusCode(500, "An error occurred while checking the ISBN.");
            }
        }
    }
}