using Microsoft.AspNetCore.Mvc;
using Web.Extensions;
using Application.DTOs;
using Application.Common;
using MediatR;
using Application.Features.Categories.Commands;
using Application.Features.Categories.Queries;
using Application.Interfaces.Services;

namespace Web.Controllers;

public class CategoryController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<CategoryController> _logger;
    private readonly IMediator _mediator;

    public CategoryController(IFileUploadService fileUploadService, ILogger<CategoryController> logger, IMediator mediator)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
        _mediator = mediator;
    }

    // GET: Category
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        try
        {
            var query = new GetCategoriesQuery(pageNumber, pageSize, searchTerm);
            var categories = await _mediator.Send(query);
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
            var category = await _mediator.Send(new GetCategoryByIdQuery(id));
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
    public async Task<IActionResult> Create(CreateCategoryDto categoryDto, IFormFile? coverImageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }

        try
        {
            var uploadResult = await this.HandleCoverImageUploadAsync(_fileUploadService, coverImageFile, "categories");
            if (!uploadResult.Success)
            {
                ModelState.AddModelError("coverImageFile", uploadResult.ErrorMessage!);
                return View(categoryDto);
            }
            if (!string.IsNullOrEmpty(uploadResult.UploadedUrl))
            {
                categoryDto.CoverImageUrl = uploadResult.UploadedUrl;
            }
            var result = await _mediator.Send(new CreateCategoryCommand(categoryDto));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to create category.");
                return View(categoryDto);
            }
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
            var category = await _mediator.Send(new GetCategoryByIdQuery(id));
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
    public async Task<IActionResult> Edit(int id, UpdateCategoryDto categoryDto, IFormFile? coverImageFile)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }
        try
        {
            var uploadResult = await this.HandleCoverImageUploadAsync(_fileUploadService, coverImageFile, "categories", categoryDto.CoverImageUrl);
            if (!uploadResult.Success)
            {
                ModelState.AddModelError("coverImageFile", uploadResult.ErrorMessage!);
                return View(categoryDto);
            }
            if (!string.IsNullOrEmpty(uploadResult.UploadedUrl))
            {
                categoryDto.CoverImageUrl = uploadResult.UploadedUrl;
            }
            var result = await _mediator.Send(new UpdateCategoryCommand(id, categoryDto));
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Error ?? "Failed to update category.");
                return View(categoryDto);
            }
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
            var category = await _mediator.Send(new GetCategoryByIdQuery(id));
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
            var result = await _mediator.Send(new DeleteCategoryCommand(id));
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error ?? "Failed to delete category.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting category ID: {CategoryId}", id);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    // POST: Category/RemoveCoverImage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCoverImage(int id)
    {
        try
        {
            var category = await _mediator.Send(new GetCategoryByIdQuery(id));
            if (category == null)
            {
                return NotFound();
            }            if (!string.IsNullOrEmpty(category.CoverImageUrl) && !category.CoverImageUrl.StartsWith("http"))
            {
                await this.SafelyRemoveCoverImageAsync(_fileUploadService, category.CoverImageUrl);
            }

            var updateDto = new UpdateCategoryDto
            {
                Name = category.Name,
                Description = category.Description,
                CoverImageUrl = null // Remove the cover image
            };

            await _mediator.Send(new UpdateCategoryCommand(id, updateDto));
            TempData["Success"] = "Cover image removed successfully.";
            
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing cover image for category ID: {CategoryId}", id);
            TempData["Error"] = "An error occurred while removing the cover image.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }
}