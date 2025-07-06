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
    /// </summary>
	public class BookController : Controller
    {
        private readonly IBookService _bookService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<BookController> _logger;
        private readonly IValidator<BookSearchRequest> _searchParametersValidator;
        private readonly IValidator<CreateBookRequest> _createBookValidator;
        private readonly IValidator<UpdateBookRequest> _updateBookValidator;

		public BookController(
            IBookService bookService,
            ICategoryService categoryService,
            ILogger<BookController> logger,
			IValidator<BookSearchRequest> searchParametersValidator,
			IValidator<CreateBookRequest> createBookValidator,
			IValidator<UpdateBookRequest> updateBookValidator)
		{
            _bookService = bookService;
            _categoryService = categoryService;
            _logger = logger;
            _searchParametersValidator = searchParametersValidator;
            _createBookValidator = createBookValidator;
            _updateBookValidator = updateBookValidator;
		}

        /// <summary>
        /// Displays book listing page with search and filter functionality.
        /// Implements UC013 (Search Books) and UC014 (Browse by Category).
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
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _bookService.GetBookByIdAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve book details: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book details");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving book details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays create book form.
        /// Implements UC010 (Add Book).
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
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(CreateBookRequest model)
        {
            try
            {
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
                    _logger.LogWarning("Failed to create book: {Error}", result.Error);
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

                _logger.LogInformation("Book created successfully: {BookId} - {BookTitle}", result.Value.Id, result.Value.Title);
                TempData["SuccessMessage"] = $"Book '{result.Value.Title}' created successfully with {model.InitialCopies} copies.";
                
                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
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
                    _logger.LogWarning("Failed to retrieve book for editing: {Error}", result.Error);
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
                _logger.LogError(ex, "Error retrieving book for editing");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes edit book request.
        /// Implements UC011 (Update Book).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(UpdateBookRequest model)
        {
            try
            {
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
                    _logger.LogWarning("Failed to update book: {Error}", result.Error);
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

                _logger.LogInformation("Book updated successfully: {BookId} - {BookTitle}", result.Value.Id, result.Value.Title);
                TempData["SuccessMessage"] = $"Book '{result.Value.Title}' updated successfully.";
                
                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book");
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
                    _logger.LogWarning("Failed to retrieve book for deletion: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // Check for active loans
                var hasLoansResult = await _bookService.BookHasActiveLoansAsync(id);
                if (hasLoansResult.IsSuccess && hasLoansResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active loans.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Check for active reservations
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
                _logger.LogError(ex, "Error retrieving book for deletion");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes delete book request.
        /// Implements UC012 (Delete Book).
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Check for active loans again (to prevent deletion if loans were created after showing the delete page)
                var hasLoansResult = await _bookService.BookHasActiveLoansAsync(id);
                if (hasLoansResult.IsSuccess && hasLoansResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active loans.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Check for active reservations again
                var hasReservationsResult = await _bookService.BookHasActiveReservationsAsync(id);
                if (hasReservationsResult.IsSuccess && hasReservationsResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete book because it has active reservations.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _bookService.DeleteBookAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete book: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }

                _logger.LogInformation("Book deleted successfully: {BookId}", id);
                TempData["SuccessMessage"] = "Book deleted successfully.";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book");
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
                _logger.LogError(ex, "Error checking if ISBN exists");
                return StatusCode(500, "An error occurred while checking the ISBN.");
            }
        }
    }
}