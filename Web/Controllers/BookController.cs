using Application.Common;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Web.Services;

namespace Web.Controllers;

/// <summary>
/// Controller for managing books in the library system.
/// Implements UC010 (Add Book), UC011 (Update Book), UC012 (Delete Book),
/// UC013 (Search Books), and UC014 (Browse by Category).
/// </summary>
/// <remarks>
/// This controller provides the web interface for book management operations.
/// All operations are properly authorized based on user roles (BR-06).
/// 
/// Use Cases Supported:
/// - UC010: Add Book - Create new books with categories and initial copies
/// - UC011: Update Book - Modify existing book information and status
/// - UC012: Delete Book - Remove books with dependency validation
/// - UC013: Search Books - Search and browse books with pagination
/// - UC014: Browse by Category - Filter books by category
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can manage books)
/// - BR-07: Book Deletion Restriction (Books with active loans/reservations cannot be deleted)
/// - BR-22: Audit Logging Requirement (All operations logged)
/// - BR-24: Role-Based Access Control (Proper authorization on all endpoints)
/// </remarks>
[Authorize]
public class BookController : Controller
{
    private readonly IBookService _bookService;
    private readonly ICategoryService _categoryService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<BookController> _logger;

    public BookController(
        IBookService bookService, 
        ICategoryService categoryService,
        IFileUploadService fileUploadService, 
        ILogger<BookController> logger)
    {
        _bookService = bookService;
        _categoryService = categoryService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    // GET: Book
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? categoryId = null)
    {
        try
        {
            var pagedRequest = new PagedRequest
            {
                PageNumber = pageNumber > 0 ? pageNumber : 1,
                PageSize = pageSize > 0 ? pageSize : 10
            };

            var books = await _bookService.SearchBooksAsync(searchTerm, null, null, null, categoryId, pagedRequest);
            return View(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving books");
            TempData["Error"] = "An error occurred while retrieving books.";
            return View(new PagedResult<BookDto>([], 0, pageNumber, pageSize));
        }
    }

    // GET: Book/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving book details for ID: {BookId}", id);
            TempData["Error"] = "An error occurred while retrieving book details.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Book/Create
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> Create()
    {
        try
        {
            // Get all categories for the dropdown
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = allCategories;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading create book page");
            TempData["Error"] = "An error occurred while loading the page.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Book/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> Create(CreateBookDto bookDto, IFormFile? coverImageFile)
    {
        if (!ModelState.IsValid)
        {
            try
            {
                var allCategories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = allCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
            }
            return View(bookDto);
        }

        try
        {
            // Handle file upload if provided
            if (coverImageFile != null && coverImageFile.Length > 0)
            {
                if (!_fileUploadService.IsValidImageFile(coverImageFile))
                {
                    ModelState.AddModelError("coverImageFile", "Please select a valid image file (jpg, jpeg, png, gif, bmp, webp).");
                    var allCategories = await _categoryService.GetAllCategoriesAsync();
                    ViewBag.Categories = allCategories;
                    return View(bookDto);
                }

                try
                {
                    var uploadedImageUrl = await _fileUploadService.UploadFileAsync(coverImageFile, "books");
                    bookDto.CoverImageUrl = uploadedImageUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading cover image for book");
                    ModelState.AddModelError("coverImageFile", "Error uploading the image. Please try again.");
                    var allCategories = await _categoryService.GetAllCategoriesAsync();
                    ViewBag.Categories = allCategories;
                    return View(bookDto);
                }
            }

            await _bookService.CreateBookAsync(bookDto);
            TempData["Success"] = "Book created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new book");
            ModelState.AddModelError("", ex.Message);
            try
            {
                var allCategories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = allCategories;
            }
            catch (Exception categoryEx)
            {
                _logger.LogError(categoryEx, "Error loading categories");
            }
            return View(bookDto);
        }
    }

    // GET: Book/Edit/5
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var updateDto = new UpdateBookDto
            {
                Title = book.Title,
                Author = book.Author,
                Publisher = book.Publisher,
                Description = book.Description,
                CoverImageUrl = book.CoverImageUrl,
                PublicationDate = book.PublicationDate,
                CategoryIds = book.CategoryDetails?.Select(c => c.Id).ToList() ?? [],
                Status = book.Status
            };

            var allCategories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = allCategories;

            return View(updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving book for edit, ID: {BookId}", id);
            TempData["Error"] = "An error occurred while retrieving the book.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Book/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> Edit(int id, UpdateBookDto bookDto, IFormFile? coverImageFile)
    {
        if (!ModelState.IsValid)
        {
            try
            {
                var allCategories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = allCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
            }
            return View(bookDto);
        }

        try
        {
            // Handle file upload if provided
            if (coverImageFile != null && coverImageFile.Length > 0)
            {
                if (!_fileUploadService.IsValidImageFile(coverImageFile))
                {
                    ModelState.AddModelError("coverImageFile", "Please select a valid image file (jpg, jpeg, png, gif, bmp, webp).");
                    var allCategories = await _categoryService.GetAllCategoriesAsync();
                    ViewBag.Categories = allCategories;
                    return View(bookDto);
                }

                try
                {
                    // Delete old image if it exists and is not a URL
                    if (!string.IsNullOrEmpty(bookDto.CoverImageUrl) && !bookDto.CoverImageUrl.StartsWith("http"))
                    {
                        await _fileUploadService.DeleteFileAsync(bookDto.CoverImageUrl);
                    }

                    var uploadedImageUrl = await _fileUploadService.UploadFileAsync(coverImageFile, "books");
                    bookDto.CoverImageUrl = uploadedImageUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading cover image for book");
                    ModelState.AddModelError("coverImageFile", "Error uploading the image. Please try again.");
                    var allCategories = await _categoryService.GetAllCategoriesAsync();
                    ViewBag.Categories = allCategories;
                    return View(bookDto);
                }
            }

            await _bookService.UpdateBookAsync(id, bookDto);
            TempData["Success"] = "Book updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating book ID: {BookId}", id);
            ModelState.AddModelError("", ex.Message);
            try
            {
                var allCategories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = allCategories;
            }
            catch (Exception categoryEx)
            {
                _logger.LogError(categoryEx, "Error loading categories");
            }
            return View(bookDto);
        }
    }

    // POST: Book/RemoveCoverImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> RemoveCoverImage(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(book.CoverImageUrl) && !book.CoverImageUrl.StartsWith("http"))
            {
                await _fileUploadService.DeleteFileAsync(book.CoverImageUrl);
            }

            var updateDto = new UpdateBookDto
            {
                Title = book.Title,
                Author = book.Author,
                Publisher = book.Publisher,
                Description = book.Description,
                CoverImageUrl = null, // Remove the cover image
                PublicationDate = book.PublicationDate,
                CategoryIds = book.CategoryDetails?.Select(c => c.Id).ToList() ?? [],
                Status = book.Status
            };

            await _bookService.UpdateBookAsync(id, updateDto);
            TempData["Success"] = "Cover image removed successfully.";
            
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing cover image for book ID: {BookId}", id);
            TempData["Error"] = "An error occurred while removing the cover image.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    // GET: Book/Delete/5
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving book for deletion, ID: {BookId}", id);
            TempData["Error"] = "An error occurred while retrieving the book.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Book/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian,Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Get book details first to delete associated image file
            var book = await _bookService.GetBookByIdAsync(id);
            if (book != null && !string.IsNullOrEmpty(book.CoverImageUrl) && !book.CoverImageUrl.StartsWith("http"))
            {
                await _fileUploadService.DeleteFileAsync(book.CoverImageUrl);
            }

            await _bookService.DeleteBookAsync(id);
            TempData["Success"] = "Book deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting book ID: {BookId}", id);
            TempData["Error"] = "An error occurred while deleting the book: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
