using Application.Common;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    // GET: Category
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        try
        {
            var pagedRequest = new PagedRequest
            {
                PageNumber = pageNumber > 0 ? pageNumber : 1,
                PageSize = pageSize > 0 ? pageSize : 10
            };

            var categories = await _categoryService.GetPaginatedCategoriesAsync(pagedRequest, searchTerm);
            return View(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving categories");
            TempData["Error"] = "An error occurred while retrieving categories.";
            return View(new PagedResult<CategoryDto>([], 0, pageNumber, pageSize));
        }
    }

    // GET: Category/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category details for ID: {CategoryId}", id);
            TempData["Error"] = "An error occurred while retrieving category details.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Category/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryDto categoryDto)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }

        try
        {
            await _categoryService.CreateCategoryAsync(categoryDto);
            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new category");
            ModelState.AddModelError("", ex.Message);
            return View(categoryDto);
        }
    }

    // GET: Category/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var updateDto = new UpdateCategoryDto
            {
                Name = category.Name,
                Description = category.Description,
                CoverImageUrl = category.CoverImageUrl
            };

            return View(updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category for edit, ID: {CategoryId}", id);
            TempData["Error"] = "An error occurred while retrieving the category.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Category/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateCategoryDto categoryDto)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }

        try
        {
            await _categoryService.UpdateCategoryAsync(id, categoryDto);
            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category ID: {CategoryId}", id);
            ModelState.AddModelError("", ex.Message);
            return View(categoryDto);
        }
    }

    // GET: Category/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category for deletion, ID: {CategoryId}", id);
            TempData["Error"] = "An error occurred while retrieving the category.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Category/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting category ID: {CategoryId}", id);
            TempData["Error"] = "An error occurred while deleting the category: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}