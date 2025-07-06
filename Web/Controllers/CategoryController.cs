using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Extensions;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for category management operations.
    /// Implements UC037 (Create Category), UC038 (Update Category), UC039 (Delete Category), 
    /// UC040 (View Category Details), and UC041 (Browse Categories).
    /// </summary>
	public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;
		private readonly IValidator<CreateCategoryRequest> _createCategoryValidator;
		private readonly IValidator<UpdateCategoryRequest> _updateCategoryValidator;
		private readonly IValidator<CategorySearchRequest> _categorySearchValidator;

		public CategoryController(
            ICategoryService categoryService,
            ILogger<CategoryController> logger,
			IValidator<CreateCategoryRequest> createCategoryValidator,
			IValidator<UpdateCategoryRequest> updateCategoryValidator,
			IValidator<CategorySearchRequest> categorySearchValidator)
		{
            _categoryService = categoryService;
            _logger = logger;
            _createCategoryValidator = createCategoryValidator;
            _updateCategoryValidator = updateCategoryValidator;
            _categorySearchValidator = categorySearchValidator;
		}

        /// <summary>
        /// Displays category listing page with optional search functionality.
        /// Implements UC041 (Browse Categories).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(CategorySearchRequest searchParams)
        {
            try
            {
                var validationResult = await _categorySearchValidator.ValidateAsync(searchParams);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    return View(new PagedResult<CategoryDto> 
                    { 
                        Page = searchParams.Page,
                        PageSize = searchParams.PageSize,
                    });
				}

				// Set default values if not provided
				searchParams.Page = searchParams.Page <= 0 ? 1 : searchParams.Page;
                searchParams.PageSize = searchParams.PageSize <= 0 ? 10 : searchParams.PageSize;

                var result = await _categoryService.GetCategoriesAsync(searchParams);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve categories: {Error}", result.Error);
                    TempData["ErrorMessage"] = "Failed to retrieve categories. Please try again later.";
                    return View(new PagedResult<CategoryDto> 
                    { 
                        Page = searchParams.Page,
                        PageSize = searchParams.PageSize,
                    });
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving categories.";
                return View();
            }
        }

        /// <summary>
        /// Displays category details page with associated books.
        /// Implements UC040 (View Category Details).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _categoryService.GetCategoryWithBooksAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve category details: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category details");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving category details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays create category form.
        /// Implements UC037 (Create Category).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes create category request.
        /// Implements UC037 (Create Category).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(CreateCategoryRequest model)
        {
            try
            {
                var validationResult = await _createCategoryValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    return View(model);
				}


				// Check if category name already exists
				var nameExistsResult = await _categoryService.CategoryNameExistsAsync(model.Name);
                if (nameExistsResult.IsSuccess && nameExistsResult.Value)
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    return View(model);
                }

                var result = await _categoryService.CreateCategoryAsync(model);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create category: {Error}", result.Error);
                    ModelState.AddModelError(string.Empty, result.Error);
                    return View(model);
                }

                _logger.LogInformation("Category created successfully: {CategoryId} - {CategoryName}", result.Value.Id, result.Value.Name);
                TempData["SuccessMessage"] = $"Category '{result.Value.Name}' created successfully.";
                
                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the category.");
                return View(model);
            }
        }

        /// <summary>
        /// Displays edit category form.
        /// Implements UC038 (Update Category).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var result = await _categoryService.GetCategoryWithBooksAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve category for editing: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                var category = result.Value;
                var updateDto = new UpdateCategoryRequest
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    CoverImageUrl = category.CoverImageUrl
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category for editing");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes edit category request.
        /// Implements UC038 (Update Category).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Edit(UpdateCategoryRequest model)
        {
            try
            {
                var validationResult = await _updateCategoryValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    return View(model);
				}

				// Check if updated name already exists (excluding current category)
				var nameExistsResult = await _categoryService.CategoryNameExistsAsync(model.Name, model.Id);
                if (nameExistsResult.IsSuccess && nameExistsResult.Value)
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    return View(model);
                }

                var result = await _categoryService.UpdateCategoryAsync(model);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to update category: {Error}", result.Error);
                    ModelState.AddModelError(string.Empty, result.Error);
                    return View(model);
                }

                _logger.LogInformation("Category updated successfully: {CategoryId} - {CategoryName}", result.Value.Id, result.Value.Name);
                TempData["SuccessMessage"] = $"Category '{result.Value.Name}' updated successfully.";
                
                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the category.");
                return View(model);
            }
        }

        /// <summary>
        /// Displays delete category confirmation page.
        /// Implements UC039 (Delete Category).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _categoryService.GetCategoryWithBooksAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to retrieve category for deletion: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // Check if category has books
                if (result.Value.Books.Count != 0)
                {
                    TempData["ErrorMessage"] = "Cannot delete category because it has books assigned to it. Reassign books to other categories first.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category for deletion");
                TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes delete category request.
        /// Implements UC039 (Delete Category).
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Check if category has books
                var hasBooksResult = await _categoryService.CategoryHasBooksAsync(id);
                if (hasBooksResult.IsSuccess && hasBooksResult.Value)
                {
                    TempData["ErrorMessage"] = "Cannot delete category because it has books assigned to it. Reassign books to other categories first.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _categoryService.DeleteCategoryAsync(id);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete category: {Error}", result.Error);
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Category deleted successfully: {CategoryId}", id);
                TempData["SuccessMessage"] = "Category deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API endpoint to check if a category name already exists.
        /// Used for client-side validation during create/edit operations.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NameExists(string name, int? id = null)
        {
            try
            {
                var result = await _categoryService.CategoryNameExistsAsync(name, id);
                return Ok(new { exists = result.IsSuccess && result.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if category name exists");
                return StatusCode(500, "An error occurred while checking the category name.");
            }
        }
    }
}