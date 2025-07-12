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
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Web.Controllers
{
	/// <summary>
	/// Controller for category management operations.
	/// Implements UC037 (Create Category), UC038 (Update Category), UC039 (Delete Category), 
	/// UC040 (View Category Details), and UC041 (Browse Categories).
	/// Business Rules: BR-11 (Category Management), BR-12 (Category Deletion Rules), BR-22 (Audit Logging), BR-24 (RBAC)
	/// </summary>
	public class CategoryController : Controller
	{
		private readonly ICategoryService _categoryService;
		private readonly IAuditService _auditService;
		private readonly ILogger<CategoryController> _logger;
		private readonly IValidator<CreateCategoryRequest> _createCategoryValidator;
		private readonly IValidator<UpdateCategoryRequest> _updateCategoryValidator;
		private readonly IValidator<CategorySearchRequest> _categorySearchValidator;

		public CategoryController(
			ICategoryService categoryService,
			IAuditService auditService,
			ILogger<CategoryController> logger,
			IValidator<CreateCategoryRequest> createCategoryValidator,
			IValidator<UpdateCategoryRequest> updateCategoryValidator,
			IValidator<CategorySearchRequest> categorySearchValidator)
		{
			_categoryService = categoryService;
			_auditService = auditService;
			_logger = logger;
			_createCategoryValidator = createCategoryValidator;
			_updateCategoryValidator = updateCategoryValidator;
			_categorySearchValidator = categorySearchValidator;
		}

		/// <summary>
		/// Displays category listing page with optional search functionality.
		/// Implements UC041 (Browse Categories).
		/// Supports BR-24 (RBAC) - accessible to all users
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Index(CategorySearchRequest? searchParams = null)
		{
			try
			{
				searchParams ??= new CategorySearchRequest { Page = 1, PageSize = 10 };
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


				var result = await _categoryService.GetCategoriesAsync(searchParams);
				if (!result.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve categories: {Error}", result.Error);
					TempData["ErrorMessage"] = result.Error;
					return View(new PagedResult<CategoryDto>());
				}

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving categories");
				TempData["ErrorMessage"] = "An error occurred while retrieving categories.";
				return View(new PagedResult<CategoryDto>());
			}
		}

		/// <summary>
		/// Displays category details page.
		/// Implements UC040 (View Category Details).
		/// Supports BR-24 (RBAC) - accessible to all users
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			try
			{
				var result = await _categoryService.GetCategoryWithBooksAsync(id);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction(nameof(Index));
				}

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving category details for {CategoryId}", id);
				TempData["ErrorMessage"] = "An error occurred while retrieving category details.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Displays create category form.
		/// Implements UC037 (Create Category).
		/// Supports BR-11 (Category Management), BR-24 (RBAC)
		/// </summary>
		[HttpGet]
		[Authorize(Roles = "Admin,Librarian")]
		public IActionResult Create()
		{
			try
			{
				return View(new CreateCategoryRequest());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create category form");
				TempData["ErrorMessage"] = "An error occurred while loading the create category form.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Processes create category request.
		/// Implements UC037 (Create Category).
		/// Supports BR-11 (Category Management), BR-22 (Audit Logging), BR-24 (RBAC)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Create(CreateCategoryRequest model, IFormFile? CoverImageFile)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

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

				// Handle cover image upload
				if (CoverImageFile != null && CoverImageFile.Length > 0)
				{
					var ext = Path.GetExtension(CoverImageFile.FileName).ToLowerInvariant();
					var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
					if (!allowed.Contains(ext))
					{
						ModelState.AddModelError("CoverImageFile", "Invalid image format. Allowed: jpg, jpeg, png, gif, webp.");
						return View(model);
					}
					var fileName = $"cat_{Guid.NewGuid()}{ext}";
					var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
					Directory.CreateDirectory(savePath);
					var filePath = Path.Combine(savePath, fileName);
					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await CoverImageFile.CopyToAsync(stream);
					}
					model.CoverImageUrl = $"/images/categories/{fileName}";
				}

				var result = await _categoryService.CreateCategoryAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				// Audit successful category creation
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Create,
					EntityType = "Category",
					EntityId = result.Value.ToString(),
					EntityName = model.Name,
					Details = $"Category created successfully: {model.Name}",
					IsSuccess = true
				});

				_logger.LogInformation("Category created successfully by user {UserId}: {CategoryName} at {Time}",
					userId, model.Name, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"Category '{model.Name}' created successfully.";
				return RedirectToAction(nameof(Details), new { id = result.Value });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating category for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while creating the category. Please try again or contact support if the problem persists.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays edit category form.
		/// Implements UC038 (Update Category).
		/// Supports BR-11 (Category Management), BR-24 (RBAC)
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
					_logger.LogWarning("Failed to retrieve category {CategoryId} for editing: {Error}", id, result.Error);
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction(nameof(Index));
				}

				var updateCategoryRequest = new UpdateCategoryRequest
				{
					Id = result.Value.Id,
					Name = result.Value.Name,
					Description = result.Value.Description,
					CoverImageUrl = result.Value.CoverImageUrl
				};

				return View(updateCategoryRequest);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading edit category form for {CategoryId}", id);
				TempData["ErrorMessage"] = "An error occurred while loading the edit category form.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Processes edit category request.
		/// Implements UC038 (Update Category).
		/// Supports BR-11 (Category Management), BR-22 (Audit Logging), BR-24 (RBAC)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Edit(UpdateCategoryRequest model, IFormFile? CoverImageFile, bool RemoveCoverImage = false)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

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

				// Handle cover image upload/removal
				if (RemoveCoverImage)
				{
					if (!string.IsNullOrEmpty(model.CoverImageUrl))
					{
						var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", model.CoverImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
						if (System.IO.File.Exists(oldPath))
						{
							System.IO.File.Delete(oldPath);
						}
					}
					model.CoverImageUrl = null;
				}
				else if (CoverImageFile != null && CoverImageFile.Length > 0)
				{
					var ext = Path.GetExtension(CoverImageFile.FileName).ToLowerInvariant();
					var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
					if (!allowed.Contains(ext))
					{
						ModelState.AddModelError("CoverImageFile", "Invalid image format. Allowed: jpg, jpeg, png, gif, webp.");
						return View(model);
					}
					var fileName = $"cat_{Guid.NewGuid()}{ext}";
					var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
					Directory.CreateDirectory(savePath);
					var filePath = Path.Combine(savePath, fileName);
					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await CoverImageFile.CopyToAsync(stream);
					}
					// Remove old image if exists
					if (!string.IsNullOrEmpty(model.CoverImageUrl))
					{
						var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", model.CoverImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
						if (System.IO.File.Exists(oldPath))
						{
							System.IO.File.Delete(oldPath);
						}
					}
					model.CoverImageUrl = $"/images/categories/{fileName}";
				}

				var result = await _categoryService.UpdateCategoryAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				// Audit successful category update
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Update,
					EntityType = "Category",
					EntityId = model.Id.ToString(),
					EntityName = model.Name,
					Details = $"Category updated successfully: {model.Name}",
					IsSuccess = true
				});

				_logger.LogInformation("Category updated successfully by user {UserId}: {CategoryId} at {Time}",
					userId, model.Id, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"Category '{model.Name}' updated successfully.";
				return RedirectToAction(nameof(Details), new { id = model.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating category {CategoryId} for user {UserId}", model.Id, User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while updating the category. Please try again or contact support if the problem persists.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays delete category confirmation page.
		/// Implements UC039 (Delete Category).
		/// Supports BR-11 (Category Management), BR-12 (Category Deletion Rules), BR-24 (RBAC)
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
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction(nameof(Index));
				}

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading delete category form for {CategoryId}", id);
				TempData["ErrorMessage"] = "An error occurred while loading the delete category form.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Processes delete category request.
		/// Implements UC039 (Delete Category).
		/// Supports BR-11 (Category Management), BR-12 (Category Deletion Rules), BR-22 (Audit Logging), BR-24 (RBAC)
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

				// Get category details for audit logging
				var categoryResult = await _categoryService.GetCategoryWithBooksAsync(id);
				var categoryName = categoryResult.IsSuccess ? categoryResult.Value.Name : "Unknown";

				var result = await _categoryService.DeleteCategoryAsync(id);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction(nameof(Details), new { id });
				}

				// Audit successful category deletion
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Delete,
					EntityType = "Category",
					EntityId = id.ToString(),
					EntityName = categoryName,
					Details = $"Category deleted successfully: {categoryName}",
					IsSuccess = true
				});

				_logger.LogInformation("Category deleted successfully by user {UserId}: {CategoryId} at {Time}",
					userId, id, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"Category '{categoryName}' deleted successfully.";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting category {CategoryId} for user {UserId}", id, User.GetUserId());
				TempData["ErrorMessage"] = "An error occurred while deleting the category.";
				return RedirectToAction(nameof(Details), new { id });
			}
		}

		/// <summary>
		/// Gets all categories for dropdown/select lists.
		/// Used by other controllers for category selection.
		/// Implements UC041 (Browse Categories) - complete list alternative flow.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetCategoriesForSelection()
		{
			try
			{
				var result = await _categoryService.GetAllCategoriesAsync();
				if (!result.IsSuccess)
				{
					return Json(new { success = false, error = result.Error });
				}

				var categories = result.Value.Select(c => new { value = c.Id, text = c.Name });
				return Json(new { success = true, categories });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving categories for selection");
				return Json(new { success = false, error = "An error occurred while retrieving categories." });
			}
		}
	}
}