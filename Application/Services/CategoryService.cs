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
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;
    private readonly IValidator<CategorySearchParametersDto> _searchValidator;
    private readonly IAuditService _auditService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator,
        IValidator<CategorySearchParametersDto> searchValidator,
        IAuditService auditService,
        ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category
    /// Implements UC037 (Create Category)
    /// </summary>
    public async Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
    {
        try
        {
            // Validate input
            var validationResult = await _createValidator.ValidateAsync(createCategoryDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Category creation validation failed: {Errors}", errors);
                return Result.Failure<CategoryDto>(errors);
            }

            // Check if category name already exists
            var nameExistsResult = await CategoryNameExistsAsync(createCategoryDto.Name);
            if (nameExistsResult.IsSuccess && nameExistsResult.Value)
            {
                _logger.LogWarning("Category creation failed: Category name '{Name}' already exists", createCategoryDto.Name);
                return Result.Failure<CategoryDto>("A category with this name already exists.");
            }

            // Map DTO to entity
            var category = _mapper.Map<Category>(createCategoryDto);
            
            // Set creation timestamp
            category.CreatedAt = DateTime.UtcNow;

            // Add to repository
            await _unitOfWork.Repository<Category>().AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Create,
                EntityType = "Category",
                EntityId = category.Id.ToString(),
                EntityName = category.Name,
                Details = $"Created new category: {category.Name}",
                AfterState = System.Text.Json.JsonSerializer.Serialize(category),
                IsSuccess = true
            });

            // Map entity to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(category);
            _logger.LogInformation("Category created successfully: {CategoryId} - {CategoryName}", category.Id, category.Name);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {Message}", ex.Message);
            return Result.Failure<CategoryDto>($"Failed to create category: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing category
    /// Implements UC038 (Update Category)
    /// </summary>
    public async Task<Result<CategoryDto>> UpdateCategoryAsync(UpdateCategoryDto updateCategoryDto)
    {
        try
        {
            // Validate input
            var validationResult = await _updateValidator.ValidateAsync(updateCategoryDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Category update validation failed: {Errors}", errors);
                return Result.Failure<CategoryDto>(errors);
            }

            // Retrieve existing category
            var category = await _unitOfWork.Repository<Category>().GetAsync(c => c.Id == updateCategoryDto.Id);
            if (category == null)
            {
                _logger.LogWarning("Category update failed: Category not found with ID {CategoryId}", updateCategoryDto.Id);
                return Result.Failure<CategoryDto>($"Category with ID {updateCategoryDto.Id} not found.");
            }

            // Check if updated name already exists (excluding current category)
            if (category.Name.ToLower() != updateCategoryDto.Name.ToLower())
            {
                var nameExistsResult = await CategoryNameExistsAsync(updateCategoryDto.Name, updateCategoryDto.Id);
                if (nameExistsResult.IsSuccess && nameExistsResult.Value)
                {
                    _logger.LogWarning("Category update failed: Category name '{Name}' already exists", updateCategoryDto.Name);
                    return Result.Failure<CategoryDto>("A category with this name already exists.");
                }
            }

            // Save original state for audit
            var originalState = System.Text.Json.JsonSerializer.Serialize(category);

            // Update entity properties
            category.Name = updateCategoryDto.Name;
            category.Description = updateCategoryDto.Description;
            category.CoverImageUrl = updateCategoryDto.CoverImageUrl;
            category.LastModifiedAt = DateTime.UtcNow;

            // Save changes
            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Update,
                EntityType = "Category",
                EntityId = category.Id.ToString(),
                EntityName = category.Name,
                Details = $"Updated category: {category.Name}",
                BeforeState = originalState,
                AfterState = System.Text.Json.JsonSerializer.Serialize(category),
                IsSuccess = true
            });

            // Map entity to DTO and return
            var categoryDto = _mapper.Map<CategoryDto>(category);
            _logger.LogInformation("Category updated successfully: {CategoryId} - {CategoryName}", category.Id, category.Name);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {Message}", ex.Message);
            return Result.Failure<CategoryDto>($"Failed to update category: {ex.Message}");
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
            if (category.Books.Any())
            {
                _logger.LogWarning("Category deletion failed: Category {CategoryId} has assigned books", id);
                return Result.Failure<bool>("Cannot delete category because it has books assigned to it. Reassign books to other categories first.");
            }

            // Save original state for audit
            var originalState = System.Text.Json.JsonSerializer.Serialize(category);
            var categoryName = category.Name;

            // Delete category
            _unitOfWork.Repository<Category>().Delete(category);
            await _unitOfWork.SaveChangesAsync();

            // Log the action
            await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
            {
                ActionType = AuditActionType.Delete,
                EntityType = "Category",
                EntityId = id.ToString(),
                EntityName = categoryName,
                Details = $"Deleted category: {categoryName}",
                BeforeState = originalState,
                IsSuccess = true
            });

            _logger.LogInformation("Category deleted successfully: {CategoryId} - {CategoryName}", id, categoryName);
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
    public async Task<Result<PaginatedCategoriesDto>> GetCategoriesAsync(CategorySearchParametersDto searchParams)
    {
        try
        {
            // Validate search parameters
            var validationResult = await _searchValidator.ValidateAsync(searchParams);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Category search validation failed: {Errors}", errors);
                return Result.Failure<PaginatedCategoriesDto>(errors);
            }

            // Set up predicate for filtering
            Expression<Func<Category, bool>>? predicate = null;
            if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            {
                var searchTerm = searchParams.SearchTerm.ToLower();
                predicate = c => c.Name.ToLower().Contains(searchTerm) || 
                                (c.Description != null && c.Description.ToLower().Contains(searchTerm));
            }

            // If pagination is not applied, get all categories
            if (!searchParams.ApplyPagination)
            {
                var allCategories = await _unitOfWork.Repository<Category>().ListAsync(
                    predicate,
                    q => q.OrderBy(c => c.Name));
                
                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(allCategories);
                
                var result = new PaginatedCategoriesDto
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
            var pagedRequest = new PagedRequest(searchParams.PageNumber, searchParams.PageSize);
            var pagedResult = await _unitOfWork.Repository<Category>().PagedListAsync(
                pagedRequest,
                predicate,
                q => q.OrderBy(c => c.Name));

            // Map to DTOs
            var categoryDtoList = _mapper.Map<IEnumerable<CategoryDto>>(pagedResult.Items);
            
            var paginatedResult = new PaginatedCategoriesDto
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
            return Result.Failure<PaginatedCategoriesDto>($"Failed to retrieve categories: {ex.Message}");
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
                predicate = c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId.Value;
            }
            else
            {
                predicate = c => c.Name.ToLower() == name.ToLower();
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

            return Result.Success(category.Books.Any());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if category has books: {Message}", ex.Message);
            return Result.Failure<bool>($"Failed to check if category has books: {ex.Message}");
        }
    }
}