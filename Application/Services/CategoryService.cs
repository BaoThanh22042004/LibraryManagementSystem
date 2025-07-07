using System.Linq.Expressions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Implementation of the ICategoryService interface.
/// Provides category management functionality supporting use cases UC037-UC041.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category
    /// Implements UC037 (Create Category)
    /// </summary>
    public async Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            // Check if category name already exists
            var nameExistsResult = await CategoryNameExistsAsync(request.Name);
            if (nameExistsResult.IsSuccess && nameExistsResult.Value)
            {
                _logger.LogWarning("Category creation failed: Category name '{Name}' already exists", request.Name);
                return Result.Failure<CategoryDto>("Category with this name already exists. Please choose a different name to proceed.");
            }

            // Map DTO to entity
            var category = _mapper.Map<Category>(request);
            
            // Set creation timestamp
            category.CreatedAt = DateTime.UtcNow;

            // Add to repository
            await _unitOfWork.Repository<Category>().AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            // Map entity to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(category);
            _logger.LogInformation("Category created successfully: {CategoryId} - {CategoryName}", category.Id, category.Name);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {Message}", ex.Message);
            return Result.Failure<CategoryDto>("System error: Unable to create category. Please try again or contact support if the problem persists.");
        }
    }

    /// <summary>
    /// Updates an existing category
    /// Implements UC038 (Update Category)
    /// </summary>
    public async Task<Result<CategoryDto>> UpdateCategoryAsync(UpdateCategoryRequest request)
    {
        try
        {
            // Retrieve existing category
            var category = await _unitOfWork.Repository<Category>().GetAsync(c => c.Id == request.Id);
            if (category == null)
            {
                _logger.LogWarning("Category update failed: Category not found with ID {CategoryId}", request.Id);
                return Result.Failure<CategoryDto>("Category not found. Please check the category and try again.");
            }

            // Check if updated name already exists (excluding current category)
            if (!category.Name.Equals(request.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                var nameExistsResult = await CategoryNameExistsAsync(request.Name, request.Id);
                if (nameExistsResult.IsSuccess && nameExistsResult.Value)
                {
                    _logger.LogWarning("Category update failed: Category name '{Name}' already exists", request.Name);
                    return Result.Failure<CategoryDto>("Category name already exists. Please choose a different name to proceed.");
                }
            }

            // Update entity properties
            category.Name = request.Name;
            category.Description = request.Description;
            category.CoverImageUrl = request.CoverImageUrl;
            category.LastModifiedAt = DateTime.UtcNow;

            // Save changes
            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync();

            // Map entity to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(category);
            _logger.LogInformation("Category updated successfully: {CategoryId} - {CategoryName}", category.Id, category.Name);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {Message}", ex.Message);
            return Result.Failure<CategoryDto>("System error: Unable to update category. Please try again or contact support if the problem persists.");
        }
    }

    /// <summary>
    /// Deletes a category by ID
    /// Implements UC039 (Delete Category)
    /// </summary>
    public async Task<Result<bool>> DeleteCategoryAsync(int id)
    {
        try
        {
            // Retrieve category
            var category = await _unitOfWork.Repository<Category>().GetAsync(
                c => c.Id == id, 
                c => c.Books); // Include books to check if category has books

            if (category == null)
            {
                _logger.LogWarning("Category deletion failed: Category not found with ID {CategoryId}", id);
                return Result.Failure<bool>($"Category with ID {id} not found.");
            }

            // Check if category has books
            if (category.Books.Count != 0)
            {
                _logger.LogWarning("Category deletion failed: Category {CategoryId} has assigned books", id);
                return Result.Failure<bool>("Cannot delete category because it has books assigned to it. Reassign books to other categories first.");
            }

            // Delete category
            _unitOfWork.Repository<Category>().Delete(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category deleted successfully: {CategoryId} - {CategoryName}", id, category.Name);
			return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to delete category: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a category by ID with associated books
    /// Implements UC040 (View Category Details)
    /// </summary>
    public async Task<Result<CategoryWithBooksDto>> GetCategoryWithBooksAsync(int id)
    {
        try
        {
            // Retrieve category with books and their copies
            var category = await _unitOfWork.Repository<Category>().GetAsync(
                c => c.Id == id,
                c => c.Books);

            if (category == null)
            {
                _logger.LogWarning("Get category with books failed: Category not found with ID {CategoryId}", id);
                return Result.Failure<CategoryWithBooksDto>($"Category with ID {id} not found.");
            }

            // Load books with their copies to calculate availability
            foreach (var book in category.Books)
            {
                await _unitOfWork.Repository<Book>().GetAsync(
                    b => b.Id == book.Id,
                    b => b.Copies);
            }

            // Map to DTO
            var categoryWithBooksDto = _mapper.Map<CategoryWithBooksDto>(category);
            
            _logger.LogInformation("Retrieved category with books: {CategoryId} - {CategoryName}, Book count: {BookCount}", 
                id, category.Name, category.Books.Count);
            
            return Result.Success(categoryWithBooksDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with books: {Message}", ex.Message);
            return Result.Failure<CategoryWithBooksDto>($"Failed to retrieve category with books: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a paginated list of categories based on search parameters
    /// Implements UC041 (Browse Categories)
    /// </summary>
    public async Task<Result<PagedResult<CategoryDto>>> GetCategoriesAsync(CategorySearchRequest request)
    {
        try
        {
            // Set up predicate for filtering
            Expression<Func<Category, bool>>? predicate = null;
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                predicate = c => c.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) || 
                                (c.Description != null && c.Description.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase));
            }

            // If pagination is not applied, get all categories
            if (!request.ApplyPagination)
            {
                var allCategories = await _unitOfWork.Repository<Category>().ListAsync(
                    predicate,
                    q => q.OrderBy(c => c.Name));
                
                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(allCategories);
                
                var result = new PagedResult<CategoryDto>
                {
                    Items = [..categoryDtos],
                    Count = categoryDtos.Count(),
                    Page = 1,
                    PageSize = categoryDtos.Count()
                };
                
                _logger.LogInformation("Retrieved all categories without pagination, Count: {Count}", result.Count);
                return Result.Success(result);
            }

            // Apply pagination
            var pagedResult = await _unitOfWork.Repository<Category>().PagedListAsync(
				request,
                predicate,
                q => q.OrderBy(c => c.Name));

            // Map to DTOs
            var categoryDtoList = _mapper.Map<IEnumerable<CategoryDto>>(pagedResult.Items);
            
            var paginatedResult = new PagedResult<CategoryDto>
            {
                Items = [..categoryDtoList],
                Count = pagedResult.Count,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };
            
            _logger.LogInformation("Retrieved paginated categories, Page: {Page}, PageSize: {PageSize}, TotalCount: {TotalCount}", 
                paginatedResult.Page, paginatedResult.PageSize, paginatedResult.Count);
            
            return Result.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories: {Message}", ex.Message);
            return Result.Failure<PagedResult<CategoryDto>>($"Failed to retrieve categories: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all categories without pagination (for dropdowns and selection lists)
    /// Implements UC041 (Browse Categories) - alternative flow for complete list
    /// </summary>
    public async Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _unitOfWork.Repository<Category>().ListAsync(
                orderBy: q => q.OrderBy(c => c.Name));
            
            var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
            
            _logger.LogInformation("Retrieved all categories, Count: {Count}", categoryDtos.Count());
            return Result.Success(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories: {Message}", ex.Message);
            return Result.Failure<IEnumerable<CategoryDto>>($"Failed to retrieve all categories: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a category name already exists (case-insensitive)
    /// Supports validation for UC037 (Create Category) and UC038 (Update Category)
    /// </summary>
    public async Task<Result<bool>> CategoryNameExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<bool>("Category name cannot be empty.");
            }

            Expression<Func<Category, bool>> predicate;
            if (excludeId.HasValue)
            {
                predicate = c => c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) && c.Id != excludeId.Value;
            }
            else
            {
                predicate = c => c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase);
            }

            var exists = await _unitOfWork.Repository<Category>().ExistsAsync(predicate);
            return Result.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if category name exists: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if category name exists: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a category has any books assigned to it
    /// Supports validation for UC039 (Delete Category)
    /// </summary>
    public async Task<Result<bool>> CategoryHasBooksAsync(int id)
    {
        try
        {
            var category = await _unitOfWork.Repository<Category>().GetAsync(
                c => c.Id == id,
                c => c.Books);

            if (category == null)
            {
                return Result.Failure<bool>($"Category with ID {id} not found.");
            }

            return Result.Success(category.Books.Count != 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if category has books: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if category has books: {ex.Message}");
        }
    }
}